using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;
using CliWrap.Internal.Extensions;

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
        /// Pipe target that discards all data.
        /// Logical equivalent to <code>/dev/null</code>.
        /// </summary>
        public static PipeTarget Null { get; } = new NullPipeTarget();

        /// <summary>
        /// Creates a pipe target that writes to a stream.
        /// </summary>
        public static PipeTarget ToStream(Stream stream, bool autoFlush) => new StreamPipeTarget(stream, autoFlush);

        /// <summary>
        /// Creates a pipe target that writes to a stream.
        /// </summary>
        // TODO: (breaking change) remove in favor of optional parameter
        public static PipeTarget ToStream(Stream stream) => ToStream(stream, true);

        /// <summary>
        /// Creates a pipe target that writes to a file.
        /// </summary>
        public static PipeTarget ToFile(string filePath) => new FilePipeTarget(filePath);

        /// <summary>
        /// Creates a pipe target that writes to a string builder.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            new StringBuilderPipeTarget(stringBuilder, encoding);

        /// <summary>
        /// Creates a pipe target that writes to a string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target that triggers a delegate on every line written.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
            new DelegatePipeTarget(handleLine, encoding);

        /// <summary>
        /// Creates a pipe target that triggers a delegate on every line written.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine) =>
            ToDelegate(handleLine, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target that triggers an asynchronous delegate on every line written.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
            new AsyncDelegatePipeTarget(handleLineAsync, encoding);

        /// <summary>
        /// Creates a pipe target that triggers an asynchronous delegate on every line written.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
            ToDelegate(handleLineAsync, Console.OutputEncoding);

        // This function needs to take output as a parameter because it's recursive
        private static void FlattenTargets(IEnumerable<PipeTarget> targets, ICollection<PipeTarget> output)
        {
            foreach (var target in targets)
            {
                if (target is MergedPipeTarget mergedTarget)
                {
                    FlattenTargets(mergedTarget.Targets, output);
                }
                else
                {
                    output.Add(target);
                }
            }
        }

        private static IReadOnlyList<PipeTarget> OptimizeTargets(IEnumerable<PipeTarget> targets)
        {
            var result = new List<PipeTarget>();

            // Unwrap merged targets
            FlattenTargets(targets, result);

            // Filter out no-op
            result.RemoveAll(t => t is NullPipeTarget);

            return result;
        }

        /// <summary>
        /// Creates a pipe target that replicates data over multiple inner targets.
        /// </summary>
        public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
        {
            // Optimize targets to avoid unnecessary work
            var optimizedTargets = OptimizeTargets(targets);

            // Avoid merging if there's only one target
            if (optimizedTargets.Count == 1)
                return optimizedTargets.Single();

            // Avoid merging if there are no targets
            if (optimizedTargets.Count == 0)
                return Null;

            return new MergedPipeTarget(optimizedTargets);
        }

        /// <summary>
        /// Creates a pipe target that replicates data over multiple inner targets.
        /// </summary>
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);
    }

    internal class NullPipeTarget : PipeTarget
    {
        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            // We need to actually exhaust the input stream to avoid potential deadlocks.
            // TODO: none of the tests fail if this is replaced with Task.CompletedTask,
            // so the above claim may be incorrect. Need to verify.
            await source.CopyToAsync(Stream.Null, cancellationToken).ConfigureAwait(false);
        }
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
            await source.CopyToAsync(_stream, _autoFlush, cancellationToken).ConfigureAwait(false);
    }

    internal class FilePipeTarget : PipeTarget
    {
        private readonly string _filePath;

        public FilePipeTarget(string filePath) => _filePath = filePath;

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var stream = File.Create(_filePath);
            await source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
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
            using var buffer = PooledBuffer.ForStreamReader();

            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer.Array, cancellationToken).ConfigureAwait(false)) > 0)
            {
                _stringBuilder.Append(buffer.Array, 0, charsRead);
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

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
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

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                await _handleAsync(line).ConfigureAwait(false);
            }
        }
    }

    internal class MergedPipeTarget : PipeTarget
    {
        public IReadOnlyList<PipeTarget> Targets { get; }

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => Targets = targets;

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            // Create a separate half-duplex stream for each target
            var targetSubStreams = new Dictionary<PipeTarget, HalfDuplexStream>();
            foreach (var target in Targets)
                targetSubStreams[target] = new HalfDuplexStream();

            try
            {
                // Start reading from those streams in background
                var readingTask = Task.WhenAll(
                    targetSubStreams.Select(async targetSubStream =>
                    {
                        var (target, subStream) = targetSubStream;
                        await target.CopyFromAsync(subStream, cancellationToken).ConfigureAwait(false);
                    })
                );

                // Read from the master stream and replicate the data to each sub-stream
                using var buffer = PooledBuffer.ForStream();
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer.Array, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    foreach (var (_, subStream) in targetSubStreams)
                        await subStream.WriteAsync(buffer.Array, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                }

                // Report that transmission is complete
                foreach (var (_, subStream) in targetSubStreams)
                    await subStream.ReportCompletionAsync(cancellationToken).ConfigureAwait(false);

                await readingTask.ConfigureAwait(false);
            }
            finally
            {
                foreach (var (_, subStream) in targetSubStreams)
                    await subStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}