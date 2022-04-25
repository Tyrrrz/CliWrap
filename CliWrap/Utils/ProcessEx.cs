using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal class ProcessEx : IDisposable
{
    private readonly Process _nativeProcess;

    private readonly TaskCompletionSource<object?> _exitTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CancellationTokenSource? _waitTimeoutCts;
    private CancellationTokenRegistration? _waitTimeoutRegistration;

    public int Id { get; private set; }

    public bool IsExited { get; private set; }

    public bool IsKilled { get; private set; }

    // We are purposely using Stream instead of StreamWriter/StreamReader to push the concerns of
    // writing and reading to PipeSource/PipeTarget at the higher level.

    public Stream StandardInput { get; private set; } = Stream.Null;

    public Stream StandardOutput { get; private set; } = Stream.Null;

    public Stream StandardError { get; private set; } = Stream.Null;

    public int ExitCode { get; private set; }

    public DateTimeOffset StartTime { get; private set; }

    public DateTimeOffset ExitTime { get; private set; }

    public ProcessEx(ProcessStartInfo startInfo) =>
        _nativeProcess = new Process { StartInfo = startInfo };

    public void Start()
    {
        Debug.Assert(Id == default, "Attempt to launch a process more than once.");

        // Hook up events
        _nativeProcess.EnableRaisingEvents = true;
        _nativeProcess.Exited += (_, _) =>
        {
            IsExited = true;
            ExitCode = _nativeProcess.ExitCode;

            // Calculate our own ExitTime to be consistent with StartTime
            ExitTime = DateTimeOffset.Now;

            // Don't cancel the task here because we don't have access to the user's cancellation token.
            // Let the upstream caller handle cancellation based on the IsKilled property.
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
        }
        catch (Win32Exception ex)
        {
            throw new Win32Exception(
                $"Failed to start a process with file path '{_nativeProcess.StartInfo.FileName}'. " +
                "Target file or working directory doesn't exist or the provided credentials are invalid.",
                ex
            );
        }

        // We can't access Process.StartTime if the process has already terminated.
        // It's entirely possible that the process exits so fast that by the time
        // we try to get the start time, it won't be accessible anymore.
        // See: https://github.com/Tyrrrz/CliWrap/issues/93
        // Calculating time ourselves is slightly inaccurate, but at least we can
        // guarantee it won't fail.
        StartTime = DateTimeOffset.Now;

        // Copy metadata
        Id = _nativeProcess.Id;
        StandardInput = _nativeProcess.StandardInput.BaseStream;
        StandardOutput = _nativeProcess.StandardOutput.BaseStream;
        StandardError = _nativeProcess.StandardError.BaseStream;
    }

    public void Kill()
    {
        try
        {
            IsKilled = true;
            _nativeProcess.Kill(true);
        }
        catch
        {
            // Ideally, we want to make sure WaitUntilExitAsync() does NOT return before the process terminates.
            // Getting an exception here could indicate an actual failure to kill the process, but could also
            // indicate that the process has already exited (race condition).
            // The exception itself doesn't carry enough information to differentiate between those cases.
            // As a workaround, we swallow all exceptions and create a registration that will mark the task as
            // completed after a timeout, to avoid waiting forever in case we were actually unable to kill the process.
            _waitTimeoutCts = new CancellationTokenSource();
            _waitTimeoutRegistration = _waitTimeoutCts.Token.Register(() =>
            {
                // Don't cancel the task here because we don't have access to the user's cancellation token
                if (_exitTcs.TrySetResult(null))
                    Debug.Fail("Process termination timed out.");
            });
            _waitTimeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
        }
    }

    public async Task WaitUntilExitAsync() => await _exitTcs.Task.ConfigureAwait(false);

    public void Dispose()
    {
        _waitTimeoutRegistration?.Dispose();
        _waitTimeoutCts?.Dispose();
        _nativeProcess.Dispose();
    }
}