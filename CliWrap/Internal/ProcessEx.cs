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

        public int Id { get; private set; }

        public StreamWriter StdIn { get; private set; } = StreamWriter.Null;

        public StreamReader StdOut { get; private set; } = StreamReader.Null;

        public StreamReader StdErr { get; private set; } = StreamReader.Null;

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
                ExitTime = DateTimeOffset.Now;
                ExitCode = _nativeProcess.ExitCode;
                _exitTcs.TrySetResult(null);
            };

            _nativeProcess.Start();
            StartTime = DateTimeOffset.Now;

            Id = _nativeProcess.Id;
            StdIn = _nativeProcess.StandardInput;
            StdOut = _nativeProcess.StandardOutput;
            StdErr = _nativeProcess.StandardError;
        }

        public bool TryKill()
        {
            try
            {
                _nativeProcess.EnableRaisingEvents = false;
                _nativeProcess.Kill(true);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _exitTcs.TrySetCanceled();
            }
        }

        public async Task WaitUntilExitAsync(CancellationToken cancellationToken = default)
        {
            using var registration = cancellationToken.Register(() => TryKill());
            await _exitTcs.Task;
        }

        public void Dispose()
        {
            // Kill the process if it's still alive at this point
            if (!_nativeProcess.HasExited)
                TryKill();

            _nativeProcess.Dispose();
        }
    }
}