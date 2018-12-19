using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class ProcessEx : IDisposable
    {
        private readonly Process _process;

        private readonly SemaphoreLock _exitLock = new SemaphoreLock();

        private readonly StringBuilder _standardOutputBuffer = new StringBuilder();
        private readonly SemaphoreLock _standardOutputEndLock = new SemaphoreLock();
        private readonly StringBuilder _standardErrorBuffer = new StringBuilder();
        private readonly SemaphoreLock _standardErrorEndLock = new SemaphoreLock();

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset ExitTime { get; private set; }

        public int ExitCode => _process.ExitCode;

        public string StandardOutput { get; private set; }

        public string StandardError { get; private set; }

        public ProcessEx(Process process,
            Action<string> standardOutputObserver = null, Action<string> standardErrorObserver = null)
        {
            _process = process;

            // Configure start info
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;

            // Wire exit event
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => _exitLock.Unlock();

            // Wire stdout
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    _standardOutputBuffer.AppendLine(args.Data);
                    standardOutputObserver?.Invoke(args.Data);
                }
                else
                {
                    // Null means end of stream
                    _standardOutputEndLock.Unlock();
                }
            };

            // Wire stderr
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    _standardErrorBuffer.AppendLine(args.Data);
                    standardErrorObserver?.Invoke(args.Data);
                }
                else
                {
                    // Null means end of stream
                    _standardErrorEndLock.Unlock();
                }
            };
        }

        public void Start()
        {
            // Start process
            _process.Start();

            // Record start time
            StartTime = DateTimeOffset.Now;

            // Being reading streams
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void PipeStandardInput(Stream stream)
        {
            // Copy stream and close stdin
            using (_process.StandardInput)
                stream.CopyTo(_process.StandardInput.BaseStream);
        }

        public async Task PipeStandardInputAsync(Stream stream)
        {
            // Copy stream and close stdin
            using (_process.StandardInput)
                await stream.CopyToAsync(_process.StandardInput.BaseStream).ConfigureAwait(false);
        }

        private void FlushBuffers()
        {
            StandardOutput = _standardOutputBuffer.ToString();
            StandardError = _standardErrorBuffer.ToString();
        }

        public void WaitForExit()
        {
            // Wait until process exit
            _exitLock.Wait();

            // Wait until streams finished reading
            _standardOutputEndLock.Wait();
            _standardErrorEndLock.Wait();

            // Record exit time
            ExitTime = DateTimeOffset.Now;

            // Flush buffers
            FlushBuffers();
        }

        public async Task WaitForExitAsync()
        {
            // Wait until process exit
            await _exitLock.WaitAsync().ConfigureAwait(false);

            // Wait until streams finished reading
            await _standardOutputEndLock.WaitAsync().ConfigureAwait(false);
            await _standardErrorEndLock.WaitAsync().ConfigureAwait(false);

            // Record exit time
            ExitTime = DateTimeOffset.Now;

            // Flush buffers
            FlushBuffers();
        }

        public bool TryKill()
        {
            try
            {
                _process.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _process.Dispose();
            _exitLock.Dispose();
            _standardOutputEndLock.Dispose();
            _standardErrorEndLock.Dispose();
        }
    }
}