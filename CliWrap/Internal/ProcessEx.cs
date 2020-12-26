using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class ProcessEx : IDisposable
    {
        private readonly Process _nativeProcess;

        private readonly TaskCompletionSource<object?> _exitTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private CancellationTokenSource? _waitTimeoutCts;
        private CancellationTokenRegistration? _waitTimeoutRegistration;

        private bool _isKilled;

        public int Id { get; private set; }

        // We are purposely using Stream instead of StreamWriter/StreamReader to push the concerns of
        // writing and reading to PipeSource/PipeTarget at the higher level.

        public Stream StdIn { get; private set; } = Stream.Null;

        public Stream StdOut { get; private set; } = Stream.Null;

        public Stream StdErr { get; private set; } = Stream.Null;

        public int ExitCode { get; private set; }

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset ExitTime { get; private set; }

        public ProcessEx(ProcessStartInfo startInfo) =>
            _nativeProcess = new Process {StartInfo = startInfo};

        public void Start()
        {
            Debug.Assert(Id == default, "Attempt to launch a process more than once.");

            // Hook up events
            _nativeProcess.EnableRaisingEvents = true;
            _nativeProcess.Exited += (sender, args) =>
            {
                // Don't rely on Process.ExitTime.
                // See the code for StartTime below for explanation.
                ExitTime = DateTimeOffset.Now;
                ExitCode = _nativeProcess.ExitCode;

                if (!_isKilled)
                {
                    _exitTcs.TrySetResult(null);
                }
                else
                {
                    _exitTcs.TrySetCanceled();
                }
            };

            // Start the process
            if (!_nativeProcess.Start())
            {
                throw new InvalidOperationException(
                    "Failed to obtain the handle when starting a process. " +
                    "This could mean that the target executable doesn't exist or that execute permission is missing."
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
            StdIn = _nativeProcess.StandardInput.BaseStream;
            StdOut = _nativeProcess.StandardOutput.BaseStream;
            StdErr = _nativeProcess.StandardError.BaseStream;
        }

        public void Kill()
        {
            try
            {
                _isKilled = true;
                _nativeProcess.Kill(true);
            }
            catch
            {
                // Ideally, we want to make sure WaitUntilExitAsync() does NOT return before the process terminates.
                // Getting an exception here could indicate an actual failure to kill the process, but could also
                // indicate that the process has already exited (race condition).
                // The exception itself doesn't carry enough information to differentiate between those cases.
                // As a workaround, we swallow all exceptions and create a delayed cancellation registration
                // that will cancel the waiting task after a timeout, preventing a potential deadlock.
                _waitTimeoutCts = new CancellationTokenSource();
                _waitTimeoutRegistration = _waitTimeoutCts.Token.Register(() =>
                {
                    if (_exitTcs.TrySetCanceled())
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
}