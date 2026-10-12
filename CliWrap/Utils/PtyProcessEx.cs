using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils.Extensions;
using PowerKit.Extensions;

namespace CliWrap.Utils;

/// <summary>
/// Process wrapper for PTY-based execution.
/// </summary>
internal class PtyProcessEx(
    PseudoTerminal pty,
    string fileName,
    string arguments,
    string workingDirectory,
    string? environmentBlock
) : IDisposable
{
    private readonly PseudoTerminal _pty = pty;
    private readonly string _fileName = fileName;
    private readonly string _arguments = arguments;
    private readonly string _workingDirectory = workingDirectory;
    private readonly string? _environmentBlock = environmentBlock;

    private readonly TaskCompletionSource _exitTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private IntPtr _processHandle;
    private int _processId;
    private volatile bool _hasExited;
    private volatile int _exitCode;
    private bool _isDisposed;

    public int Id => _processId;

    public string Name => Path.GetFileName(_fileName);

    /// <summary>
    /// Gets the stream for writing to the process's stdin via PTY.
    /// </summary>
    public Stream StandardInput => _pty.InputStream;

    /// <summary>
    /// Gets the stream for reading from the process's stdout via PTY.
    /// </summary>
    /// <remarks>
    /// With PTY, stderr is merged into stdout by the terminal.
    /// </remarks>
    public Stream StandardOutput => _pty.OutputStream;

    /// <summary>
    /// Gets the stream for reading from the process's stderr via PTY.
    /// </summary>
    /// <remarks>
    /// With PTY, stderr is merged into stdout by the terminal,
    /// so this returns the same stream as <see cref="StandardOutput"/>.
    /// </remarks>
    public Stream StandardError => _pty.OutputStream;

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset ExitTime { get; private set; }

    public int ExitCode => _exitCode;

    public void Start()
    {
        if (OperatingSystem.IsWindows())
        {
            StartWindows();
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            StartUnix();
        }
        else
        {
            throw new PlatformNotSupportedException(
                "PTY process execution is not supported on this platform."
            );
        }

        StartTime = DateTimeOffset.Now;

        // Start background task to wait for process exit
        _ = Task.Run(WaitForExitBackground);
    }

    [SupportedOSPlatform("windows")]
    private void StartWindows()
    {
        var windowsPty = (WindowsPseudoTerminal)_pty;

        string? applicationName = null;
        var commandLine = string.IsNullOrWhiteSpace(_arguments)
            ? $"\"{_fileName}\""
            : $"\"{_fileName}\" {_arguments}";

        if (
            Path.GetExtension(_fileName).Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(_fileName).Equals(".bat", StringComparison.OrdinalIgnoreCase)
        )
        {
            applicationName = Environment.GetEnvironmentVariable("ComSpec");
            if (string.IsNullOrWhiteSpace(applicationName))
                applicationName = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            var batchCommand = commandLine;
            commandLine = $"\"{applicationName}\" /d /s /c \"{batchCommand}\"";
        }

        // CreateProcessW may modify lpCommandLine, so it must receive mutable storage.
        var commandLineBuffer = (commandLine + '\0').ToCharArray();

        // Initialize process thread attribute list
        var attributeListSize = IntPtr.Zero;
        NativeMethods.Windows.InitializeProcThreadAttributeList(
            IntPtr.Zero,
            1,
            0,
            ref attributeListSize
        );

        var attributeList = Marshal.AllocHGlobal(attributeListSize);
        try
        {
            if (
                !NativeMethods.Windows.InitializeProcThreadAttributeList(
                    attributeList,
                    1,
                    0,
                    ref attributeListSize
                )
            )
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Add pseudo console attribute
            if (
                !NativeMethods.Windows.UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    NativeMethods.Windows.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                    windowsPty.Handle,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero
                )
            )
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Prepare startup info
            // Set STARTF_USESTDHANDLES with null handles to prevent the child from
            // inheriting the parent's console handles (fixes issue when parent output
            // is redirected). See: https://github.com/microsoft/terminal/issues/11276
            var startupInfo = new NativeMethods.Windows.StartupInfoEx
            {
                StartupInfo = new NativeMethods.Windows.StartupInfo
                {
                    cb = Marshal.SizeOf<NativeMethods.Windows.StartupInfoEx>(),
                    dwFlags = NativeMethods.Windows.STARTF_USESTDHANDLES,
                    hStdInput = IntPtr.Zero,
                    hStdOutput = IntPtr.Zero,
                    hStdError = IntPtr.Zero,
                },
                lpAttributeList = attributeList,
            };

            // Prepare environment block if provided
            var envPtr = IntPtr.Zero;
            if (_environmentBlock != null)
            {
                envPtr = Marshal.StringToHGlobalUni(_environmentBlock);
            }

            try
            {
                // Create the process with pseudo console
                var creationFlags = NativeMethods.Windows.EXTENDED_STARTUPINFO_PRESENT;
                if (envPtr != IntPtr.Zero)
                {
                    creationFlags |= NativeMethods.Windows.CREATE_UNICODE_ENVIRONMENT;
                }

                var workDir = string.IsNullOrEmpty(_workingDirectory) ? null : _workingDirectory;

                if (
                    !NativeMethods.Windows.CreateProcessW(
                        applicationName,
                        commandLineBuffer,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        false,
                        creationFlags,
                        envPtr,
                        workDir,
                        ref startupInfo,
                        out var processInfo
                    )
                )
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        $"Failed to create process with PTY: {_fileName}"
                    );
                }

                _processHandle = processInfo.hProcess;
                _processId = processInfo.dwProcessId;

                // Close the thread handle immediately - we don't need it
                if (processInfo.hThread != IntPtr.Zero)
                {
                    NativeMethods.Windows.CloseHandle(processInfo.hThread);
                }
            }
            finally
            {
                if (envPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(envPtr);
            }
        }
        finally
        {
            NativeMethods.Windows.DeleteProcThreadAttributeList(attributeList);
            Marshal.FreeHGlobal(attributeList);
        }
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private void StartUnix()
    {
        var unixPty = (UnixPseudoTerminal)_pty;

        // Allocate posix_spawn structures
        var fileActionsPtr = Marshal.AllocHGlobal(NativeMethods.Unix.PosixSpawnFileActionsSize);
        var attrPtr = Marshal.AllocHGlobal(NativeMethods.Unix.PosixSpawnAttrSize);

        try
        {
            // Initialize file actions
            var result = NativeMethods.Unix.PosixSpawnFileActionsInit(fileActionsPtr);
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"posix_spawn_file_actions_init failed with error {result}"
                );
            }

            try
            {
                // Initialize spawn attributes
                result = NativeMethods.Unix.PosixSpawnAttrInit(attrPtr);
                if (result != 0)
                {
                    throw new InvalidOperationException(
                        $"posix_spawnattr_init failed with error {result}"
                    );
                }

                try
                {
                    if (!string.IsNullOrEmpty(_workingDirectory))
                    {
                        result = NativeMethods.Unix.PosixSpawnFileActionsAddChDir(
                            fileActionsPtr,
                            _workingDirectory
                        );
                        ThrowIfPosixSpawnActionFailed(result, "addchdir_np");
                    }

                    // The child becomes a session leader before file actions run. Close the
                    // inherited slave and reopen its path as stdin so the PTY becomes the
                    // controlling terminal, then duplicate it to stdout and stderr.
                    if (unixPty.SlaveFd > 2)
                    {
                        result = NativeMethods.Unix.PosixSpawnFileActionsAddClose(
                            fileActionsPtr,
                            unixPty.SlaveFd
                        );
                        ThrowIfPosixSpawnActionFailed(result, "addclose (slave)");
                    }

                    result = NativeMethods.Unix.PosixSpawnFileActionsAddOpen(
                        fileActionsPtr,
                        0,
                        unixPty.SlaveName,
                        NativeMethods.Unix.O_RDWR,
                        0
                    );
                    ThrowIfPosixSpawnActionFailed(result, "addopen (stdin)");

                    result = NativeMethods.Unix.PosixSpawnFileActionsAddDup2(fileActionsPtr, 0, 1);
                    ThrowIfPosixSpawnActionFailed(result, "adddup2 (stdout)");

                    result = NativeMethods.Unix.PosixSpawnFileActionsAddDup2(fileActionsPtr, 0, 2);
                    ThrowIfPosixSpawnActionFailed(result, "adddup2 (stderr)");

                    if (unixPty.MasterFd > 2)
                    {
                        result = NativeMethods.Unix.PosixSpawnFileActionsAddClose(
                            fileActionsPtr,
                            unixPty.MasterFd
                        );
                        ThrowIfPosixSpawnActionFailed(result, "addclose (master)");
                    }

                    result = NativeMethods.Unix.PosixSpawnAttrSetFlags(
                        attrPtr,
                        NativeMethods.Unix.POSIX_SPAWN_SETSID
                    );
                    if (result != 0)
                    {
                        throw new InvalidOperationException(
                            $"posix_spawnattr_setflags failed with error {result}"
                        );
                    }

                    var argv = BuildArgv();
                    var envp = BuildEnvp();

                    result = NativeMethods.Unix.PosixSpawnp(
                        out _processId,
                        _fileName,
                        fileActionsPtr,
                        attrPtr,
                        argv,
                        envp
                    );

                    if (result != 0)
                    {
                        throw new Win32Exception(
                            result,
                            $"Failed to spawn process with PTY: {_fileName}"
                        );
                    }
                }
                finally
                {
                    NativeMethods.Unix.PosixSpawnAttrDestroy(attrPtr);
                }
            }
            finally
            {
                NativeMethods.Unix.PosixSpawnFileActionsDestroy(fileActionsPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fileActionsPtr);
            Marshal.FreeHGlobal(attrPtr);
        }

        // Close slave fd in parent - only master is needed for I/O
        unixPty.CloseSlave();
    }

    private static void ThrowIfPosixSpawnActionFailed(int error, string action)
    {
        if (error != 0)
            throw new InvalidOperationException(
                $"posix_spawn_file_actions_{action} failed with error {error}"
            );
    }

    private string[] BuildArgv()
    {
        // First element is the program name
        if (string.IsNullOrWhiteSpace(_arguments))
        {
            return [_fileName, null!];
        }

        // Parse arguments (simple space-separated, respecting quotes)
        var args = ParseArguments(_arguments);
        var argv = new string[args.Count + 2];
        argv[0] = _fileName;
        for (var i = 0; i < args.Count; i++)
        {
            argv[i + 1] = args[i];
        }
        argv[argv.Length - 1] = null!; // NULL terminator
        return argv;
    }

    private static List<string> ParseArguments(string arguments)
    {
        // This parser handles the escaping format produced by ArgumentsBuilder.Escape(),
        // which follows the Windows command-line escaping convention:
        // - Arguments containing spaces or quotes are wrapped in double quotes
        // - Double quotes inside arguments are escaped as \"
        // - Backslashes before quotes are doubled (\\")
        // - Backslashes not before quotes are kept as-is

        var args = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var argumentStarted = false;

        for (var i = 0; i < arguments.Length; i++)
        {
            var c = arguments[i];

            if (inQuotes)
            {
                if (c == '\\' && i + 1 < arguments.Length)
                {
                    var next = arguments[i + 1];
                    if (next == '"')
                    {
                        // Escaped quote - add the quote and skip the backslash
                        current.Append('"');
                        i++;
                    }
                    else if (next == '\\')
                    {
                        // Check if this sequence of backslashes is followed by a quote
                        var backslashCount = 0;
                        var j = i;
                        while (j < arguments.Length && arguments[j] == '\\')
                        {
                            backslashCount++;
                            j++;
                        }

                        if (j < arguments.Length && arguments[j] == '"')
                        {
                            // Backslashes before quote: each pair becomes one backslash
                            current.Append('\\', backslashCount / 2);
                            if (backslashCount % 2 == 1)
                            {
                                // Odd number means the quote is escaped
                                current.Append('"');
                                i = j; // Skip past the quote
                            }
                            else
                            {
                                // Even number means the quote ends the string
                                i = j - 1; // Position at last backslash, loop will handle quote
                            }
                        }
                        else
                        {
                            // Backslashes not before quote - keep as-is
                            current.Append(c);
                        }
                    }
                    else
                    {
                        // Backslash not followed by quote or backslash - keep as-is
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    // End of quoted section
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
                    argumentStarted = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (argumentStarted)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                        argumentStarted = false;
                    }
                }
                else
                {
                    current.Append(c);
                    argumentStarted = true;
                }
            }
        }

        if (argumentStarted)
        {
            args.Add(current.ToString());
        }

        return args;
    }

    private string[] BuildEnvp()
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(_environmentBlock))
        {
            foreach (
                System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables()
            )
            {
                env[(string)entry.Key] = (string)entry.Value!;
            }

            env["TERM"] = "xterm-256color";
        }

        if (!string.IsNullOrEmpty(_environmentBlock))
        {
            foreach (
                var pair in _environmentBlock.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            )
            {
                var idx = pair.IndexOf('=');
                if (idx > 0)
                {
                    var key = pair.Substring(0, idx);
                    var value = pair.Substring(idx + 1);
                    env[key] = value;
                }
            }
        }

        // Convert to envp format (KEY=VALUE strings, NULL-terminated array)
        var envp = new string[env.Count + 1];
        var i = 0;
        foreach (var kvp in env)
        {
            envp[i++] = $"{kvp.Key}={kvp.Value}";
        }
        envp[env.Count] = null!; // NULL terminator
        return envp;
    }

    private void WaitForExitBackground()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var waitResult = NativeMethods.Windows.WaitForSingleObject(
                    _processHandle,
                    NativeMethods.Windows.INFINITE
                );
                if (waitResult == NativeMethods.Windows.WAIT_FAILED)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (waitResult != NativeMethods.Windows.WAIT_OBJECT_0)
                    throw new InvalidOperationException(
                        $"Unexpected process wait result: 0x{waitResult:X8}."
                    );

                if (!NativeMethods.Windows.GetExitCodeProcess(_processHandle, out var exitCode))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                _exitCode = (int)exitCode;
            }
            else
            {
                // Wait for process exit using waitpid
                while (true)
                {
                    var result = NativeMethods.Unix.WaitPid(_processId, out var status, 0);
                    if (result == _processId)
                    {
                        if (NativeMethods.Unix.WIFEXITED(status))
                        {
                            _exitCode = NativeMethods.Unix.WEXITSTATUS(status);
                        }
                        else if (NativeMethods.Unix.WIFSIGNALED(status))
                        {
                            // Process killed by signal - convention: 128 + signal number
                            _exitCode = 128 + NativeMethods.Unix.WTERMSIG(status);
                        }
                        else
                        {
                            _exitCode = -1;
                        }
                        break;
                    }
                    else if (result == -1)
                    {
                        var error = Marshal.GetLastWin32Error();
                        // Retry if interrupted by signal
                        if (error == NativeMethods.Unix.EINTR)
                            continue;

                        throw new Win32Exception(error);
                    }

                    throw new InvalidOperationException($"Unexpected waitpid result: {result}.");
                }
            }

            ExitTime = DateTimeOffset.Now;
            _hasExited = true;

            // ConPTY must be closed to signal EOF. Unix keeps the master open so buffered
            // terminal output can drain; its last slave close produces EOF/EIO naturally.
            try
            {
                _pty.CloseConsole();
            }
            catch
            {
                // CloseConsole is a best-effort cleanup signal; failing here mustn't mask exit.
            }

            _exitTcs.TrySetResult();
        }
        catch (Exception ex)
        {
            _exitTcs.TrySetException(ex);
        }
    }

    /// <summary>
    /// Sends an interrupt signal to the process.
    /// </summary>
    /// <remarks>
    /// On Unix, this sends SIGINT directly to the process. Writing Ctrl+C (0x03) to the PTY
    /// is unreliable because it requires the terminal to be in cooked mode with ISIG enabled,
    /// and the child must be the controlling process of the terminal.
    /// On Windows, this writes Ctrl+C to the PTY input, which ConPTY handles.
    /// </remarks>
    public void Interrupt()
    {
        if (OperatingSystem.IsWindows() && _hasExited)
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // On Windows, write Ctrl+C to the PTY - ConPTY handles signal delivery
                var ctrlC = new byte[] { 0x03 };
                _pty.InputStream.Write(ctrlC, 0, 1);
                _pty.InputStream.Flush();
            }
            else
            {
                // The PTY child is a session and process-group leader. Signal the whole group
                // so descendants cannot outlive cancellation or keep the slave open.
                NativeMethods.Unix.Kill(-_processId, NativeMethods.Unix.SIGINT);
            }
        }
        catch (IOException)
        {
            Kill();
        }
        catch (ObjectDisposedException)
        {
            Kill();
        }
    }

    /// <summary>
    /// Terminates the process.
    /// </summary>
    public void Kill()
    {
        if (OperatingSystem.IsWindows() && _hasExited)
            return;

        if (OperatingSystem.IsWindows())
            NativeMethods.Windows.TerminateProcess(_processHandle, 1);
        else
            NativeMethods.Unix.Kill(-_processId, NativeMethods.Unix.SIGKILL);
    }

    public async Task WaitUntilExitAsync(CancellationToken cancellationToken = default)
    {
        await _exitTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _pty.Dispose();

        // Close process handle on Windows (thread handle is closed immediately after process creation)
        if (OperatingSystem.IsWindows() && _processHandle != IntPtr.Zero)
            NativeMethods.Windows.CloseHandle(_processHandle);

        _isDisposed = true;
    }
}
