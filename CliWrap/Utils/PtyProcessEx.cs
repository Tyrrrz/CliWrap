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

namespace CliWrap.Utils;

/// <summary>
/// Process wrapper for PTY-based execution.
/// </summary>
internal class PtyProcessEx : IProcessEx
{
    // Lock for thread-safe working directory changes on Unix
    private static readonly object _unixWorkingDirectoryLock = new();

    private readonly PseudoTerminal _pty;
    private readonly string _fileName;
    private readonly string _arguments;
    private readonly string _workingDirectory;
    private readonly string? _environmentBlock;

    private readonly TaskCompletionSource _exitTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private IntPtr _processHandle;
    private int _processId;
    private volatile bool _hasExited;
    private volatile int _exitCode;
    private bool _disposed;

    public PtyProcessEx(
        PseudoTerminal pty,
        string fileName,
        string arguments,
        string workingDirectory,
        string? environmentBlock
    )
    {
        _pty = pty;
        _fileName = fileName;
        _arguments = arguments;
        _workingDirectory = workingDirectory;
        _environmentBlock = environmentBlock;
    }

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

        // Build command line - executable path must be quoted, arguments follow
        var commandLine = string.IsNullOrWhiteSpace(_arguments)
            ? $"\"{_fileName}\""
            : $"\"{_fileName}\" {_arguments}";

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
                        null,
                        commandLine,
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
        var slaveFd = unixPty.SlaveFd;

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
                    // Set up file descriptor redirections:
                    // Redirect stdin (0), stdout (1), stderr (2) to the PTY slave
                    result = NativeMethods.Unix.PosixSpawnFileActionsAddDup2(
                        fileActionsPtr,
                        slaveFd,
                        0
                    );
                    if (result != 0)
                    {
                        throw new InvalidOperationException(
                            $"posix_spawn_file_actions_adddup2 (stdin) failed with error {result}"
                        );
                    }

                    result = NativeMethods.Unix.PosixSpawnFileActionsAddDup2(
                        fileActionsPtr,
                        slaveFd,
                        1
                    );
                    if (result != 0)
                    {
                        throw new InvalidOperationException(
                            $"posix_spawn_file_actions_adddup2 (stdout) failed with error {result}"
                        );
                    }

                    result = NativeMethods.Unix.PosixSpawnFileActionsAddDup2(
                        fileActionsPtr,
                        slaveFd,
                        2
                    );
                    if (result != 0)
                    {
                        throw new InvalidOperationException(
                            $"posix_spawn_file_actions_adddup2 (stderr) failed with error {result}"
                        );
                    }

                    // Close the original slave fd in the child (after dup2)
                    result = NativeMethods.Unix.PosixSpawnFileActionsAddClose(
                        fileActionsPtr,
                        slaveFd
                    );
                    if (result != 0)
                    {
                        throw new InvalidOperationException(
                            $"posix_spawn_file_actions_addclose failed with error {result}"
                        );
                    }

                    // On Linux, try to set POSIX_SPAWN_SETSID to create new session
                    if (OperatingSystem.IsLinux())
                    {
                        NativeMethods.Unix.PosixSpawnAttrSetFlags(
                            attrPtr,
                            NativeMethods.Unix.POSIX_SPAWN_SETSID
                        );
                    }

                    // Build argv array
                    var argv = BuildArgv();

                    // Build envp array
                    var envp = BuildEnvp();

                    // Lock around working directory change since it's process-wide
                    lock (_unixWorkingDirectoryLock)
                    {
                        var originalDir = Environment.CurrentDirectory;
                        if (!string.IsNullOrEmpty(_workingDirectory))
                        {
                            Environment.CurrentDirectory = _workingDirectory;
                        }

                        try
                        {
                            // Spawn the process
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
                            if (!string.IsNullOrEmpty(_workingDirectory))
                            {
                                Environment.CurrentDirectory = originalDir;
                            }
                        }
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
        var args = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var c in arguments)
        {
            if (inQuotes)
            {
                if (c == quoteChar)
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
                if (c == '"' || c == '\'')
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
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
        {
            args.Add(current.ToString());
        }

        return args;
    }

    private string[] BuildEnvp()
    {
        // Start with current environment
        var env = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            env[(string)entry.Key] = (string)entry.Value!;
        }

        // Set TERM for PTY
        env["TERM"] = "xterm-256color";

        // Parse and add custom environment variables
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

    private async Task WaitForExitBackground()
    {
        await Task.Run(() =>
        {
            if (OperatingSystem.IsWindows())
            {
                NativeMethods.Windows.WaitForSingleObject(
                    _processHandle,
                    NativeMethods.Windows.INFINITE
                );

                if (NativeMethods.Windows.GetExitCodeProcess(_processHandle, out var exitCode))
                {
                    _exitCode = (int)exitCode;
                }
            }
            else
            {
                // Wait for process exit using waitpid
                while (true)
                {
                    var result = NativeMethods.Unix.WaitPid(_processId, out var status, 0);
                    if (result == _processId)
                    {
                        // Process exited - extract exit code from status
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
                        // Other error (process may have already been reaped)
                        break;
                    }
                }
            }

            ExitTime = DateTimeOffset.Now;
            _hasExited = true;
            _exitTcs.TrySetResult();
        });
    }

    /// <summary>
    /// Sends Ctrl+C to the process through the PTY.
    /// </summary>
    public void Interrupt()
    {
        try
        {
            var ctrlC = new byte[] { 0x03 };
            _pty.InputStream.Write(ctrlC, 0, 1);
            _pty.InputStream.Flush();
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
        if (_hasExited)
            return;

        if (OperatingSystem.IsWindows())
            NativeMethods.Windows.TerminateProcess(_processHandle, 1);
        else
            NativeMethods.Unix.Kill(_processId, 9);
    }

    public async Task WaitUntilExitAsync(CancellationToken cancellationToken = default)
    {
        await using (
            cancellationToken
                .Register(() => _exitTcs.TrySetCanceled(cancellationToken))
                .ToAsyncDisposable()
        )
        {
            await _exitTcs.Task.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Closes the PTY console to signal EOF on the output stream.
    /// </summary>
    /// <remarks>
    /// Call this after the process exits to allow output reading to complete.
    /// </remarks>
    public void CloseConsole()
    {
        _pty.CloseConsole();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _pty.Dispose();

        // Close process handle on Windows (thread handle is closed immediately after process creation)
        if (OperatingSystem.IsWindows() && _processHandle != IntPtr.Zero)
            NativeMethods.Windows.CloseHandle(_processHandle);

        _disposed = true;
    }
}
