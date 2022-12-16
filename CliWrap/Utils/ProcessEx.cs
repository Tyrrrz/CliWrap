using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal class ProcessEx : IDisposable
{
    private readonly Process _nativeProcess;

    private readonly TaskCompletionSource<object?> _exitTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Timer? _waitForExitTimeoutTimer;

    public int Id => _nativeProcess.Id;

    public string Name =>
        // Can't rely on ProcessName because it becomes inaccessible after the process exits
        Path.GetFileName(_nativeProcess.StartInfo.FileName);

    // We are purposely using Stream instead of StreamWriter/StreamReader to push the concerns of
    // writing and reading to PipeSource/PipeTarget at the higher level.

    public Stream StandardInput => _nativeProcess.StandardInput.BaseStream;

    public Stream StandardOutput => _nativeProcess.StandardOutput.BaseStream;

    public Stream StandardError => _nativeProcess.StandardError.BaseStream;

    // We have to keep track of StartTime ourselves because it becomes inaccessible after the process exits
    // https://github.com/Tyrrrz/CliWrap/issues/93
    public DateTimeOffset StartTime { get; private set; }

    // We have to keep track of ExitTime ourselves because it becomes inaccessible after the process exits
    // https://github.com/Tyrrrz/CliWrap/issues/93
    public DateTimeOffset ExitTime { get; private set; }

    public int ExitCode => _nativeProcess.ExitCode;

    public ProcessEx(ProcessStartInfo startInfo) =>
        _nativeProcess = new Process { StartInfo = startInfo };

    public void Start()
    {
        // Hook up events
        _nativeProcess.EnableRaisingEvents = true;
        _nativeProcess.Exited += (_, _) =>
        {
            ExitTime = DateTimeOffset.Now;
            _exitTcs.TrySetResult(null);
        };

        // Start the process
        try
        {
            if (!_nativeProcess.Start())
            {
                throw new InvalidOperationException(
                    $"Failed to start a process with file path '{_nativeProcess.StartInfo.FileName}'. " +
                    "Target file is not an executable or lacks execute permissions."
                );
            }

            StartTime = DateTimeOffset.Now;
        }
        catch (Win32Exception ex)
        {
            throw new Win32Exception(
                $"Failed to start a process with file path '{_nativeProcess.StartInfo.FileName}'. " +
                "Target file or working directory doesn't exist, or the provided credentials are invalid.",
                ex
            );
        }
    }

    // Sends SIGINT
    public void Interrupt()
    {
        bool TryInterrupt()
        {
            try
            {
                // On Windows, we need to launch an external executable that will attach
                // to the target process's console (or create one if it doesn't exist),
                // and then send a Ctrl+C event to it. We can guarantee that the target
                // process doesn't have a console window because we're creating the
                // process with the CREATE_NO_WINDOW flag set. This ensures that only
                // the target process will receive the signal, and not any other process
                // spawned by us.
                // https://github.com/Tyrrrz/CliWrap/issues/47
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var signaler = WindowsSignaler.Deploy();
                    return signaler.TrySend(_nativeProcess.Id, 0);
                }

                // On Unix, we can just send the signal to the process directly
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return NativeMethods.Unix.Kill(_nativeProcess.Id, 2) == 0;
                }

                // Unsupported platform
                return false;
            }
            catch
            {
                return false;
            }
        }

        if (!TryInterrupt())
        {
            // In case of failure, revert to the default behavior of killing the process.
            // Ideally, we should throw an exception here, but this method is called from
            // a cancellation callback upstream, so we can't do that.
            Kill();
            Debug.Fail("Failed to send an interrupt signal.");
        }
    }

    // Sends SIGKILL
    public void Kill()
    {
        try
        {
            _nativeProcess.Kill(true);
        }
        catch when (_nativeProcess.HasExited)
        {
            // The process has exited before we could kill it. This is fine.
        }
        catch
        {
            // Either we actually failed to kill the process, or the system hasn't finished processing
            // the kill request yet. In either case, we can't really do anything about it.
            // Set up a timeout that will resolve the task after a while, in case the process hasn't exited by then.
            _waitForExitTimeoutTimer = new Timer(
                _ =>
                {
                    // We don't cancel the task here. Proper exception will be thrown upstream.
                    if (_exitTcs.TrySetResult(null))
                        Debug.Fail("Process termination timed out.");
                },
                null,
                // Trigger in X seconds
                TimeSpan.FromSeconds(3),
                // Don't repeat
                Timeout.InfiniteTimeSpan
            );
        }
    }

    public async Task WaitUntilExitAsync() => await _exitTcs.Task.ConfigureAwait(false);

    public void Dispose()
    {
        _waitForExitTimeoutTimer?.Dispose();
        _nativeProcess.Dispose();
    }
}