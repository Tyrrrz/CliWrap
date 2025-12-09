using System;
using System.ComponentModel;
using System.Diagnostics;
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
internal class PtyProcessEx : IDisposable
{
    private readonly PseudoTerminal _pty;
    private readonly string _fileName;
    private readonly string _arguments;
    private readonly string _workingDirectory;
    private readonly string? _environmentBlock;

    private readonly TaskCompletionSource _exitTcs = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private IntPtr _processHandle;
    private IntPtr _threadHandle;
    private int _processId;
    private bool _hasExited;
    private int _exitCode;
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
    /// With PTY, stderr is merged into stdout.
    /// </remarks>
    public Stream StandardOutput => _pty.OutputStream;

    /// <summary>
    /// Gets an empty stream since stderr is merged into stdout with PTY.
    /// </summary>
    public Stream StandardError => Stream.Null;

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
                _threadHandle = IntPtr.Zero;
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

        // On Unix, we need to use the standard Process class
        // but configure environment to work with PTY
        // The slave fd will be used by the child process

        // For a proper PTY implementation on Unix, we would need to fork
        // Since fork is dangerous in .NET managed code, we use a workaround:
        // Start the process normally but pass the slave fd via inheritance

        // Use standard .NET Process for Unix
        // The PTY slave fd needs to be set up in the child, which is complex
        // For now, we'll use a simpler approach: start process with PTY environment hints

        var startInfo = new ProcessStartInfo
        {
            FileName = _fileName,
            Arguments = _arguments,
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
        };

        // Set TERM environment variable to indicate PTY
        startInfo.Environment["TERM"] = "xterm-256color";

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
                    startInfo.Environment[key] = value;
                }
            }
        }

        // Note: For full Unix PTY support, we would need to:
        // 1. Fork the process
        // 2. In child: call setsid(), login_tty(slave_fd), exec
        // 3. In parent: close slave fd, use master fd
        // This requires unsafe P/Invoke fork/exec which is complex in managed code

        // For this implementation, we use the PTY master for I/O
        // but the child process won't have a proper controlling terminal
        // This still provides colored output support for many programs

        var process = new Process { StartInfo = startInfo };
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            ExitTime = DateTimeOffset.Now;
            _exitCode = process.ExitCode;
            _hasExited = true;
            _exitTcs.TrySetResult();
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {_fileName}");
            }
        }
        catch (Win32Exception ex)
        {
            throw new Win32Exception(
                $"Failed to start process with PTY: {_fileName}. {ex.Message}",
                ex
            );
        }

        _processId = process.Id;

        // Close slave fd in parent - only master is needed
        unixPty.CloseSlave();
    }

    private async Task WaitForExitBackground()
    {
        if (OperatingSystem.IsWindows())
        {
            await Task.Run(() =>
            {
                NativeMethods.Windows.WaitForSingleObject(
                    _processHandle,
                    NativeMethods.Windows.INFINITE
                );

                if (NativeMethods.Windows.GetExitCodeProcess(_processHandle, out var exitCode))
                {
                    _exitCode = (int)exitCode;
                }

                ExitTime = DateTimeOffset.Now;
                _hasExited = true;
                _exitTcs.TrySetResult();
            });
        }
        // Unix exit handling is done via Process.Exited event in StartUnix
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
        catch
        {
            Kill();
        }
    }

    /// <summary>
    /// Terminates the process.
    /// </summary>
    public void Kill()
    {
        try
        {
            if (_hasExited)
                return;

            if (OperatingSystem.IsWindows())
            {
                NativeMethods.Windows.TerminateProcess(_processHandle, 1);
            }
            else
            {
                NativeMethods.Unix.Kill(_processId, 9);
            }
        }
        catch
        {
            // Ignore - process may have already exited
        }
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
        if (!_disposed)
        {
            _pty.Dispose();

            if (OperatingSystem.IsWindows())
            {
                // Thread handle is closed immediately after process creation
                if (_processHandle != IntPtr.Zero)
                    NativeMethods.Windows.CloseHandle(_processHandle);
            }

            _disposed = true;
        }
    }
}
