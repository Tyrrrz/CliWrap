using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Native;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap;

/// <summary>
/// Internal unified process abstraction.  Exposes a common surface regardless of whether the
/// process was started normally or under a pseudo-terminal.
/// </summary>
internal sealed class NativeProcess : IDisposable
{
    // ── Common properties ──────────────────────────────────────────────────────────────────────

    public int Id { get; }
    public string Name { get; }
    public Stream StandardInput { get; }
    public Stream StandardOutput { get; }

    // For PTY processes, stderr is merged into stdout by the terminal.  We expose Stream.Null so
    // the unified execution loop can drive the user's stderr pipe to completion without special-
    // casing the PTY path.
    public Stream StandardError { get; }

    public DateTimeOffset StartTime { get; }
    public DateTimeOffset ExitTime { get; private set; }
    public int ExitCode { get; private set; }

    // Signalled when the process exits (set by the background wait task).
    private readonly TaskCompletionSource _exitTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    // Cleanup actions that must run after the process exits (e.g. close ConPTY handle).
    private readonly Action? _dispose;

    // Kill / interrupt delegates — differ between normal and PTY paths.
    private readonly Action _kill;
    private readonly Action _interrupt;

    private NativeProcess(
        int id,
        string name,
        Stream standardInput,
        Stream standardOutput,
        Stream standardError,
        DateTimeOffset startTime,
        Action kill,
        Action interrupt,
        Action? dispose = null
    )
    {
        Id = id;
        Name = name;
        StandardInput = standardInput;
        StandardOutput = standardOutput;
        StandardError = standardError;
        StartTime = startTime;
        _kill = kill;
        _interrupt = interrupt;
        _dispose = dispose;
    }

    // ── Public operations ──────────────────────────────────────────────────────────────────────

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
            return _exitTcs.Task;

        // Wrap so that cancellation propagates without completing the real TCS.
        return _exitTcs.Task.WaitAsync(cancellationToken);
    }

    public void Kill() => _kill();

    public void Interrupt() => _interrupt();

    public void Dispose()
    {
        StandardInput.Dispose();
        StandardOutput.Dispose();
        StandardError.Dispose();
        _dispose?.Invoke();
    }

    // Internal — called by the background wait loop when the process exits.
    private void CompleteExit(int exitCode)
    {
        ExitCode = exitCode;
        ExitTime = DateTimeOffset.Now;
        _exitTcs.TrySetResult();
    }

    // ── Factory: normal (ProcessStartInfo) path ────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="NativeProcess" /> from an already-started
    /// <see cref="System.Diagnostics.Process" />.  The streams are the redirected BCL streams.
    /// </summary>
    public static NativeProcess FromProcess(Process process)
    {
        var startTime = DateTimeOffset.Now;
        var name = Path.GetFileName(process.StartInfo.FileName);

        var native = new NativeProcess(
            id: process.Id,
            name: name,
            standardInput: process.StandardInput.BaseStream,
            standardOutput: process.StandardOutput.BaseStream,
            standardError: process.StandardError.BaseStream,
            startTime: startTime,
            kill: () =>
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort
                }
            },
            interrupt: () =>
            {
                try
                {
                    process.TryInterrupt();
                }
                catch
                {
                    // Best-effort
                }
            },
            dispose: process.Dispose
        );

        // Wait for exit on a background thread so we don't block the event loop.
        _ = Task.Run(async () =>
        {
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            native.CompleteExit(process.ExitCode);
        });

        return native;
    }

    // ── Factory: PTY path ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="NativeProcess" /> running under a pseudo-terminal.
    /// Dispatches to the correct platform implementation automatically.
    /// </summary>
    public static NativeProcess CreatePty(NativeProcessStartInfo startInfo)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                throw new PlatformNotSupportedException(
                    "Pseudo-terminal support requires Windows 10 version 1809 (build 17763) or later. "
                        + $"Current version: {Environment.OSVersion.Version}."
                );
            }

            return CreatePtyWindows(startInfo);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return CreatePtyUnix(startInfo);

        throw new PlatformNotSupportedException(
            $"Pseudo-terminal support is not available on {Environment.OSVersion.Platform}."
        );
    }

    // ── PTY on Unix (forkpty + execvp) ────────────────────────────────────────────────────────

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static NativeProcess CreatePtyUnix(NativeProcessStartInfo startInfo)
    {
        var winsize = new PtyNativeMethods.Unix.WinSize
        {
            Col = (ushort)startInfo.PseudoConsoleOptions!.Columns,
            Row = (ushort)startInfo.PseudoConsoleOptions!.Rows,
        };

        // forkpty: creates master/slave pty pair, forks, and makes slave the controlling
        // terminal of the child.  Returns child PID in the parent, 0 in the child.
        var pid = PtyNativeMethods.Unix.ForkPty(out var masterFd, ref winsize);

        if (pid < 0)
            throw new InvalidOperationException(
                $"forkpty failed. Error: {Marshal.GetLastWin32Error()}"
            );

        if (pid == 0)
        {
            // ── Child process ──────────────────────────────────────────────────────────────────
            // We are in the forked child.  The slave PTY is already our controlling terminal
            // (forkpty called login_tty internally).  We only need to chdir and exec.

            // Set environment variables
            foreach (var (key, value) in startInfo.EnvironmentVariables)
            {
                if (value is not null)
                    Environment.SetEnvironmentVariable(key, value);
                else
                    Environment.SetEnvironmentVariable(key, null);
            }

            // Set TERM so the child knows it has a real terminal
            if (!startInfo.EnvironmentVariables.ContainsKey("TERM"))
                Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Change working directory
            if (!string.IsNullOrEmpty(startInfo.WorkingDirectory))
                PtyNativeMethods.Unix.Chdir(startInfo.WorkingDirectory);

            // Build argv: [ program, arg1, arg2, ..., null ]
            var argv = BuildUnixArgv(startInfo.FileName, startInfo.Arguments);

            // Replace this process image with the target executable.
            // execvp searches PATH if the name has no directory component.
            PtyNativeMethods.Unix.ExecVp(startInfo.FileName, argv);

            // execvp only returns on failure.
            PtyNativeMethods.Unix.Exit(127);
            return null!; // unreachable
        }

        // ── Parent process ─────────────────────────────────────────────────────────────────────

        // Mark the master fd as close-on-exec so it isn't inherited by future forks.
        PtyNativeMethods.Unix.Fcntl(
            masterFd,
            PtyNativeMethods.Unix.F_SETFD,
            PtyNativeMethods.Unix.FD_CLOEXEC
        );

        var masterStream = new UnixPtyFdStream(masterFd);
        var startTime = DateTimeOffset.Now;

        // Obtain a managed Process object so we can use WaitForExitAsync / ExitCode.
        // GetProcessById works because forkpty gave us the child PID.
        var childProcess = Process.GetProcessById(pid);

        var native = new NativeProcess(
            id: pid,
            name: Path.GetFileName(startInfo.FileName),
            standardInput: masterStream,
            standardOutput: masterStream,
            standardError: Stream.Null,
            startTime: startTime,
            kill: () =>
            {
                try
                {
                    childProcess.Kill(entireProcessTree: true);
                }
                catch { }
            },
            interrupt: () =>
            {
                try
                {
                    NativeMethods.Unix.Kill(
                        pid,
                        2 /* SIGINT */
                    );
                }
                catch { }
            },
            dispose: () =>
            {
                masterStream.Dispose();
                childProcess.Dispose();
            }
        );

        _ = Task.Run(async () =>
        {
            await childProcess.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            native.CompleteExit(childProcess.ExitCode);
        });

        return native;
    }

    private static string?[] BuildUnixArgv(string fileName, string arguments)
    {
        // argv[0] = program name, followed by parsed arguments, terminated by null
        if (string.IsNullOrWhiteSpace(arguments))
            return [fileName, null];

        var parts = ParseArguments(arguments);
        var argv = new string?[parts.Count + 2];
        argv[0] = fileName;
        for (var i = 0; i < parts.Count; i++)
            argv[i + 1] = parts[i];
        argv[argv.Length - 1] = null;
        return argv;
    }

    // ── PTY on Windows (ConPTY + CreateProcessW) ──────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    private static NativeProcess CreatePtyWindows(NativeProcessStartInfo startInfo)
    {
        // Create pipe pair for PTY input (parent write → child stdin)
        if (
            !PtyNativeMethods.Windows.CreatePipe(
                out var inputRead,
                out var inputWrite,
                IntPtr.Zero,
                0
            )
        )
            throw new InvalidOperationException(
                $"Failed to create PTY input pipe. Error: {Marshal.GetLastWin32Error()}"
            );

        // Create pipe pair for PTY output (child stdout/stderr → parent read)
        if (
            !PtyNativeMethods.Windows.CreatePipe(
                out var outputRead,
                out var outputWrite,
                IntPtr.Zero,
                0
            )
        )
        {
            inputRead.Dispose();
            inputWrite.Dispose();
            throw new InvalidOperationException(
                $"Failed to create PTY output pipe. Error: {Marshal.GetLastWin32Error()}"
            );
        }

        // Create the pseudo console. ConPTY takes ownership of inputRead and outputWrite.
        var size = new PtyNativeMethods.Windows.Coord
        {
            X = (short)startInfo.PseudoConsoleOptions!.Columns,
            Y = (short)startInfo.PseudoConsoleOptions!.Rows,
        };

        var hr = PtyNativeMethods.Windows.CreatePseudoConsole(
            size,
            inputRead,
            outputWrite,
            0,
            out var hPC
        );

        // Close the ends now owned by ConPTY.
        inputRead.Dispose();
        outputWrite.Dispose();

        if (hr != 0)
        {
            inputWrite.Dispose();
            outputRead.Dispose();
            throw new InvalidOperationException($"CreatePseudoConsole failed. HRESULT: 0x{hr:X8}");
        }

        // Build the process thread attribute list carrying the ConPTY handle.
        var attributeListSize = IntPtr.Zero;
        PtyNativeMethods.Windows.InitializeProcThreadAttributeList(
            IntPtr.Zero,
            1,
            0,
            ref attributeListSize
        );

        var attributeList = Marshal.AllocHGlobal(attributeListSize);
        try
        {
            if (
                !PtyNativeMethods.Windows.InitializeProcThreadAttributeList(
                    attributeList,
                    1,
                    0,
                    ref attributeListSize
                )
            )
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (
                !PtyNativeMethods.Windows.UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    PtyNativeMethods.Windows.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                    hPC,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero
                )
            )
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // STARTUPINFOEX with null std handles — prevents parent console handle leaks.
            // See: https://github.com/microsoft/terminal/issues/11276
            var startupInfo = new PtyNativeMethods.Windows.StartupInfoEx
            {
                StartupInfo = new PtyNativeMethods.Windows.StartupInfo
                {
                    cb = Marshal.SizeOf<PtyNativeMethods.Windows.StartupInfoEx>(),
                    dwFlags = PtyNativeMethods.Windows.STARTF_USESTDHANDLES,
                    hStdInput = IntPtr.Zero,
                    hStdOutput = IntPtr.Zero,
                    hStdError = IntPtr.Zero,
                },
                lpAttributeList = attributeList,
            };

            // Build command line string
            var commandLine = string.IsNullOrWhiteSpace(startInfo.Arguments)
                ? $"\"{startInfo.FileName}\""
                : $"\"{startInfo.FileName}\" {startInfo.Arguments}";

            // Build environment block (null-separated Unicode string pairs)
            var envPtr = BuildWindowsEnvironmentBlock(startInfo.EnvironmentVariables);

            uint creationFlags = PtyNativeMethods.Windows.EXTENDED_STARTUPINFO_PRESENT;
            if (envPtr != IntPtr.Zero)
                creationFlags |= PtyNativeMethods.Windows.CREATE_UNICODE_ENVIRONMENT;

            try
            {
                if (
                    !PtyNativeMethods.Windows.CreateProcessW(
                        null,
                        commandLine,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        creationFlags,
                        envPtr,
                        string.IsNullOrEmpty(startInfo.WorkingDirectory)
                            ? null
                            : startInfo.WorkingDirectory,
                        ref startupInfo,
                        out var processInfo
                    )
                )
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        $"Failed to create process: {startInfo.FileName}"
                    );

                // Close thread handle immediately — we don't use it.
                if (processInfo.hThread != IntPtr.Zero)
                    PtyNativeMethods.Windows.CloseHandle(processInfo.hThread);

                var pid = processInfo.dwProcessId;
                var hProcess = processInfo.hProcess;
                var startTime = DateTimeOffset.Now;

                var inputStream = new FileStream(
                    inputWrite,
                    FileAccess.Write,
                    bufferSize: 4096,
                    isAsync: false
                );
                var outputStream = new FileStream(
                    outputRead,
                    FileAccess.Read,
                    bufferSize: 4096,
                    isAsync: false
                );

                var native = new NativeProcess(
                    id: pid,
                    name: Path.GetFileName(startInfo.FileName),
                    standardInput: inputStream,
                    standardOutput: outputStream,
                    standardError: Stream.Null,
                    startTime: startTime,
                    kill: () =>
                    {
                        if (hProcess != IntPtr.Zero)
                            PtyNativeMethods.Windows.TerminateProcess(hProcess, 1);
                    },
                    interrupt: () =>
                    {
                        // Writing Ctrl+C (0x03) to the PTY input pipe causes ConPTY to deliver
                        // the interrupt signal to the child process.
                        try
                        {
                            inputStream.WriteByte(0x03);
                            inputStream.Flush();
                        }
                        catch { }
                    },
                    dispose: () =>
                    {
                        inputStream.Dispose();
                        outputStream.Dispose();
                        PtyNativeMethods.Windows.ClosePseudoConsole(hPC);
                        if (hProcess != IntPtr.Zero)
                            PtyNativeMethods.Windows.CloseHandle(hProcess);
                    }
                );

                // Background wait
                _ = Task.Run(() =>
                {
                    PtyNativeMethods.Windows.WaitForSingleObject(
                        hProcess,
                        PtyNativeMethods.Windows.INFINITE
                    );
                    PtyNativeMethods.Windows.GetExitCodeProcess(hProcess, out var exitCode);

                    // Close the ConPTY so that outputStream reaches EOF, allowing
                    // the output draining task to complete cleanly.
                    PtyNativeMethods.Windows.ClosePseudoConsole(hPC);

                    native.CompleteExit((int)exitCode);
                });

                return native;
            }
            finally
            {
                if (envPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(envPtr);
            }
        }
        finally
        {
            PtyNativeMethods.Windows.DeleteProcThreadAttributeList(attributeList);
            Marshal.FreeHGlobal(attributeList);
        }
    }

    [SupportedOSPlatform("windows")]
    private static IntPtr BuildWindowsEnvironmentBlock(
        IReadOnlyDictionary<string, string?> overrides
    )
    {
        if (overrides.Count == 0)
            return IntPtr.Zero;

        // Start from the current environment and apply overrides.
        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
            env[(string)entry.Key] = (string?)entry.Value;

        foreach (var (key, value) in overrides)
        {
            if (value is not null)
                env[key] = value;
            else
                env.Remove(key);
        }

        // Build the block as: KEY=VALUE\0KEY=VALUE\0\0
        // Allocate as a char array and pin it via Marshal.
        var chars = new System.Text.StringBuilder();
        foreach (var (k, v) in env)
        {
            chars.Append(k);
            chars.Append('=');
            chars.Append(v ?? string.Empty);
            chars.Append('\0');
        }
        chars.Append('\0'); // double-null terminator

        var str = chars.ToString();
        var ptr = Marshal.AllocHGlobal(str.Length * 2); // UTF-16
        Marshal.Copy(str.ToCharArray(), 0, ptr, str.Length);
        return ptr;
    }

    // ── Shared helpers ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses an arguments string using the Windows command-line quoting convention that
    /// <see cref="CliWrap.Builders.ArgumentsBuilder" /> produces.
    /// </summary>
    private static List<string> ParseArguments(string arguments)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < arguments.Length; i++)
        {
            var c = arguments[i];

            if (inQuotes)
            {
                if (c == '\\' && i + 1 < arguments.Length && arguments[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }
}
