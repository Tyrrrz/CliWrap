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

        private readonly TaskCompletionSource<object?> _exitTcs = new TaskCompletionSource<object?>();
        private readonly TaskCompletionSource<object?> _stdOutCompleteTcs = new TaskCompletionSource<object?>();
        private readonly TaskCompletionSource<object?> _stdErrCompleteTcs = new TaskCompletionSource<object?>();

        private bool _isReading;

        public event EventHandler<string>? StdOutReceived;

        public event EventHandler<string>? StdErrReceived;

        public event EventHandler? StdOutCompleted;

        public event EventHandler? StdErrCompleted;

        public int Id { get; private set; }

        public int ExitCode { get; private set; }

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset ExitTime { get; private set; }

        public ProcessEx(ProcessStartInfo startInfo)
        {
            _nativeProcess = new Process {StartInfo = startInfo};
        }

        public void Start()
        {
            _nativeProcess.EnableRaisingEvents = true;
            _nativeProcess.Exited += (sender, args) =>
            {
                _exitTcs.TrySetResult(null);
                ExitTime = DateTimeOffset.Now;
                ExitCode = _nativeProcess.ExitCode;
            };

            _nativeProcess.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    StdOutReceived?.Invoke(this, args.Data);
                }
                else
                {
                    _stdOutCompleteTcs.TrySetResult(null);
                    StdOutCompleted?.Invoke(this, EventArgs.Empty);
                }
            };

            _nativeProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    StdErrReceived?.Invoke(this, args.Data);
                }
                else
                {
                    _stdErrCompleteTcs.TrySetResult(null);
                    StdErrCompleted?.Invoke(this, EventArgs.Empty);
                }
            };

            _nativeProcess.Start();
            StartTime = DateTimeOffset.Now;
            Id = _nativeProcess.Id;

            _nativeProcess.BeginOutputReadLine();
            _nativeProcess.BeginErrorReadLine();

            _isReading = true;
        }

        public async Task PipeStandardInputAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using (_nativeProcess.StandardInput)
                await source.CopyToAsync(_nativeProcess.StandardInput.BaseStream, cancellationToken);
        }

        public async Task WaitUntilExitAsync(CancellationToken cancellationToken = default)
        {
            using var registration = cancellationToken.Register(() => TryKill());
            await Task.WhenAll(_exitTcs.Task, _stdOutCompleteTcs.Task, _stdErrCompleteTcs.Task);
        }

        public bool TryKill()
        {
            try
            {
                _nativeProcess.EnableRaisingEvents = false;
                _nativeProcess.Kill();

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _exitTcs.TrySetCanceled();
                _stdOutCompleteTcs.TrySetCanceled();
                _stdErrCompleteTcs.TrySetCanceled();
            }
        }

        public void Dispose()
        {
            // Unsubscribe from process events
            // (process may still trigger events even after getting disposed)
            _nativeProcess.EnableRaisingEvents = false;
            if (_isReading)
            {
                _nativeProcess.CancelOutputRead();
                _nativeProcess.CancelErrorRead();

                _isReading = false;
            }

            _nativeProcess.Dispose();
        }
    }
}