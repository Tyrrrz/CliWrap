using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using CliWrap.Exceptions;
using CliWrap.Internal;

namespace CliWrap
{
    public class StreamingCli
    {
        private readonly string _filePath;
        private readonly CliConfiguration _configuration;
        private readonly Stream _input;

        public StreamingCli(string filePath, CliConfiguration configuration, Stream input)
        {
            _filePath = filePath;
            _configuration = configuration;
            _input = input;
        }

        public async IAsyncEnumerable<StreamingItem> ExecuteAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var process = new ProcessEx(_configuration.GetStartInfo(_filePath));

            var channel = new Channel<StreamingItem>(1000);
            var stdOutPublisher = channel.CreatePublisher();
            var stdErrPublisher = channel.CreatePublisher();

            process.StdOutReceived += (sender, s) => stdOutPublisher.Publish(new StreamingItem(StandardStream.StandardOutput, s));
            process.StdErrReceived += (sender, s) => stdErrPublisher.Publish(new StreamingItem(StandardStream.StandardError, s));
            process.StdOutCompleted += (sender, args) => stdOutPublisher.Dispose();
            process.StdErrCompleted += (sender, args) => stdErrPublisher.Dispose();

            process.Start();
            await process.PipeStandardInputAsync(_input, cancellationToken);

            while (true)
            {
                if (channel.TryGetNext(out var item))
                    yield return item;
                else if (channel.Publishers > 0)
                    await channel.WaitUntilNextAsync();
                else break;
            }

            await process.WaitUntilExitAsync(cancellationToken);

            if (_configuration.IsExitCodeValidationEnabled && process.ExitCode != 0)
                throw CliExecutionException.ExitCodeValidation(_filePath, _configuration.Arguments, process.ExitCode);
        }
    }
}