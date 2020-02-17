using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Internal;

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
            var channel = new Channel<StreamingItem>(1000);

            var process = new Process
            {
                StartInfo = _configuration.GetStartInfo(_filePath)
            };
            process.Start();

            channel.RegisterListener();
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    channel.Send(new StreamingItem(StandardStream.StandardOutput, args.Data));
                }
                else
                {
                    channel.UnregisterListener();
                }
            };

            channel.RegisterListener();
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    channel.Send(new StreamingItem(StandardStream.StandardError, args.Data));
                else
                {
                    channel.UnregisterListener();
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (true)
            {
                if (channel.TryGetNext(out var item))
                    yield return item;
                else if (channel.Publishers > 0)
                    await channel.WaitUntilNextAsync();
                else break;
            }

            if (_configuration.IsExitCodeValidationEnabled && process.ExitCode != 0)
                throw CliExecutionException.ExitCodeValidation(_filePath, _configuration.Arguments, process.ExitCode);
        }
    }
}