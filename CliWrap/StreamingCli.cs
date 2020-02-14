using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace CliWrap
{
    public class StreamingCli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;

        public StreamingCli(string filePath, CliConfiguration configuration)
        {
            _filePath = filePath;
            _configuration = configuration;
        }

        public async IAsyncEnumerable<StreamingItem> ExecuteAsync()
        {
            var queue = new ConcurrentQueue<StreamingItem>();
            var isStdOutDone = false;
            var isStdErrDone = false;

            var process = new Process
            {
                StartInfo = _configuration.GetStartInfo(_filePath)
            };
            process.Start();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    queue.Enqueue(new StreamingItem(StandardStream.StandardOutput, args.Data));
                else
                    isStdOutDone = true;
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    queue.Enqueue(new StreamingItem(StandardStream.StandardError, args.Data));
                else
                    isStdErrDone = true;
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            StreamingItem item = null;
            while ((!isStdOutDone || !isStdErrDone) || queue.TryDequeue(out item))
            {
                if (item != null)
                    yield return item;
            }
        }
    }
}