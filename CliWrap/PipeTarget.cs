using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;

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
        public static PipeTarget ToStream(Stream stream, bool autoFlush) => new StreamPipeTarget(stream, autoFlush);

        /// <summary>
        /// Creates a pipe target from a writeable stream.
        /// </summary>
        // TODO: change to optional argument when breaking changes are ok
        public static PipeTarget ToStream(Stream stream) => ToStream(stream, true);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            new StringBuilderPipeTarget(stringBuilder, encoding);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
            new DelegatePipeTarget(handleLine, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine) =>
            ToDelegate(handleLine, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
            new AsyncDelegatePipeTarget(handleLineAsync, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
            ToDelegate(handleLineAsync, Console.OutputEncoding);

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
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>)targets);

        /// <summary>
        /// Pipe target that ignores all data.
        /// </summary>
        public static PipeTarget Null { get; } = ToStream(Stream.Null);

        /// <summary>
        /// Pipe target that pipes directly to corresponding output stream (stdout or stderr) of parent process.
        /// </summary>
        public static PipeTarget ParentProcess { get; } = new ParentProcessPipeTarget();
    }

    internal class StreamPipeTarget : PipeTarget
    {
        private readonly Stream _stream;
        private readonly bool _autoFlush;

        public StreamPipeTarget(Stream stream, bool autoFlush)
        {
            _stream = stream;
            _autoFlush = autoFlush;
        }

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            await source.CopyToAsync(_stream, _autoFlush, cancellationToken);
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
            using var reader = new StreamReader(source, _encoding, false, BufferSizes.StreamReader, true);

            var buffer = new char[BufferSizes.StreamReader];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
            {
                _stringBuilder.Append(buffer, 0, charsRead);
            }
        }
    }

    internal class DelegatePipeTarget : PipeTarget
    {
        private readonly Action<string> _handle;
        private readonly Encoding _encoding;

        public DelegatePipeTarget(Action<string> handle, Encoding encoding)
        {
            _handle = handle;
            _encoding = encoding;
        }

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(source, _encoding, false, BufferSizes.StreamReader, true);

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken))
            {
                _handle(line);
            }
        }
    }

    internal class AsyncDelegatePipeTarget : PipeTarget
    {
        private readonly Func<string, Task> _handleAsync;
        private readonly Encoding _encoding;

        public AsyncDelegatePipeTarget(Func<string, Task> handleAsync, Encoding encoding)
        {
            _handleAsync = handleAsync;
            _encoding = encoding;
        }

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(source, _encoding, false, BufferSizes.StreamReader, true);

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken))
            {
                await _handleAsync(line);
            }
        }
    }

    internal class MergedPipeTarget : PipeTarget
    {
        private readonly IReadOnlyList<PipeTarget> _targets;

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => _targets = targets;

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            // Create separate half-duplex sub-streams for each target
            var subStreams = _targets
                .Select(_ => new HalfDuplexStream())
                .ToArray();

            // Start piping from those streams
            var targetTasks = _targets
                .Zip(subStreams, async (target, subStream) => await target.CopyFromAsync(subStream, cancellationToken))
                .ToArray();

            // Read from master stream and write data to sub-streams
            var buffer = new byte[BufferSizes.Stream];
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                foreach (var subStream in subStreams)
                    await subStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            }

            // Report that transmission is complete
            foreach (var subStream in subStreams)
                await subStream.ReportCompletionAsync(cancellationToken);

            // Wait until all tasks complete so that it's safe to dispose streams
            await Task.WhenAll(targetTasks);

            // Cleanup
            foreach (var subStream in subStreams)
                await subStream.DisposeAsync();
        }
    }

    internal class ParentProcessPipeTarget : PipeTarget
    {
        public override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Stream copy operation is not supported to parent process.");
    }
}