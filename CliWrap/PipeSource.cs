using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;

namespace CliWrap
{
    public abstract partial class PipeSource
    {
        public abstract Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default);
    }

    public partial class PipeSource
    {
        public static PipeSource FromStream(Stream stream) => new StreamPipeSource(stream);

        public static PipeSource FromBytes(byte[] bytes) => FromStream(bytes.ToStream());

        public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

        public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);

        public static PipeSource FromCli(Cli cli) => new CliPipeSource(cli);

        public static PipeSource Null { get; } = FromStream(Stream.Null);
    }

    internal class StreamPipeSource : PipeSource
    {
        private readonly Stream _stream;

        public StreamPipeSource(Stream stream)
        {
            _stream = stream;
        }

        public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            _stream.CopyToAsync(destination, cancellationToken);
    }

    internal class CliPipeSource : PipeSource
    {
        private readonly Cli _cli;

        public CliPipeSource(Cli cli)
        {
            _cli = cli;
        }

        public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            await _cli.WithStandardOutputPipe(PipeTarget.ToStream(destination)).ExecuteAsync(cancellationToken);
    }
}