using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap
{
    /// <summary>
    /// Abstraction that represents an outwards-facing pipe.
    /// </summary>
    public abstract partial class PipeTarget
    {
        /// <summary>
        /// Copies the binary content from the stream and pushes it into the pipe.
        /// </summary>
        public abstract Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default);
    }

    public partial class PipeTarget
    {
        /// <summary>
        /// Creates a pipe target from a writeable stream.
        /// </summary>
        public static PipeTarget ToStream(Stream stream) => new StreamPipeTarget(stream);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            new StringBuilderPipeTarget(stringBuilder, encoding);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to encode the string into byte stream.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> lineHandler, Encoding encoding) =>
            new DelegatePipeTarget(lineHandler, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to encode the string into byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> lineHandler) =>
            ToDelegate(lineHandler, Console.OutputEncoding);

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
        {
            var actualTargets = targets.Where(t => t != Null).ToArray();

            if (actualTargets.Length == 1)
                return actualTargets.Single();

            if (actualTargets.Length == 0)
                return Null;

            return new MergedPipeTarget(actualTargets);
        }

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);

        /// <summary>
        /// Pipe target that ignores all data.
        /// </summary>
        public static PipeTarget Null { get; } = ToStream(Stream.Null);
    }

    internal class StreamPipeTarget : PipeTarget
    {
        private readonly Stream _stream;

        public StreamPipeTarget(Stream stream) => _stream = stream;

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

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => _targets = targets;

        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            Task.WhenAll(_targets.Select(t => t.CopyFromAsync(source, cancellationToken)));
    }
}