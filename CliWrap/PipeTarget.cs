using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap
{
    public abstract partial class PipeTarget
    {
        public abstract Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default);
    }

    public partial class PipeTarget
    {
        public static PipeTarget ToStream(Stream stream) => new StreamPipeTarget(stream);

        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            new StringBuilderPipeTarget(stringBuilder, encoding);

        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        public static PipeTarget ToDelegate(Action<string> lineHandler, Encoding encoding) =>
            new DelegatePipeTarget(lineHandler, encoding);

        public static PipeTarget ToDelegate(Action<string> lineHandler) =>
            ToDelegate(lineHandler, Console.OutputEncoding);

        public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
        {
            var actualTargets = targets.Where(t => t != Null).ToArray();

            if (actualTargets.Length == 1)
                return actualTargets.Single();

            if (actualTargets.Length == 0)
                return Null;

            return new MergedPipeTarget(actualTargets);
        }

        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);

        public static PipeTarget Null { get; } = ToStream(Stream.Null);
    }

    internal class StreamPipeTarget : PipeTarget
    {
        private readonly Stream _stream;

        public StreamPipeTarget(Stream stream)
        {
            _stream = stream;
        }

        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            source.CopyToAsync(_stream, cancellationToken);
    }

    internal class StringBuilderPipeTarget : PipeTarget
    {
        private readonly StringBuilder _stringBuilder;
        private readonly Encoding _encoding;

        public StringBuilderPipeTarget(StringBuilder stringBuilder, Encoding encoding)
        {
            _stringBuilder = stringBuilder;
            _encoding = encoding;
        }

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken);
            _stringBuilder.Append(_encoding.GetChars(buffer.ToArray()));
        }
    }

    internal class DelegatePipeTarget : PipeTarget
    {
        private readonly Action<string> _handler;
        private readonly Encoding _encoding;

        public DelegatePipeTarget(Action<string> handler, Encoding encoding)
        {
            _handler = handler;
            _encoding = encoding;
        }

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(source, _encoding, false, 1024, true);
            string line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                // TODO: HANDLE CANCELLATION!
                _handler(line);
            }
        }
    }

    internal class MergedPipeTarget : PipeTarget
    {
        private readonly IReadOnlyList<PipeTarget> _targets;

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets)
        {
            _targets = targets;
        }

        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            Task.WhenAll(_targets.Select(t => t.CopyFromAsync(source, cancellationToken)));
    }
}