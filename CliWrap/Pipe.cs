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
    /// Factory methods for creating <see cref="PipeSource"/> and <see cref="PipeTarget"/> instances.
    /// </summary>
    public static partial class Pipe
    {
        private static readonly PipeSource NullPipeSource = new StreamPipeSource(Stream.Null, false);
        private static readonly PipeTarget NullPipeTarget = new StreamPipeTarget(Stream.Null, false);

        /// <summary>
        /// Creates an empty pipe source.
        /// </summary>
        public static PipeSource FromNull() => NullPipeSource;

        /// <summary>
        /// Creates a pipe target that discards all data.
        /// </summary>
        public static PipeTarget ToNull() => NullPipeTarget;
    }

    public static partial class Pipe
    {
        private class StreamPipeSource : PipeSource
        {
            private readonly Stream _stream;
            private readonly bool _autoFlush;

            public StreamPipeSource(Stream stream, bool autoFlush)
            {
                _stream = stream;
                _autoFlush = autoFlush;
            }

            public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
                await _stream.CopyToAsync(destination, _autoFlush, cancellationToken);
        }

        /// <summary>
        /// Creates a pipe source that reads from a stream.
        /// </summary>
        public static PipeSource FromStream(Stream stream, bool autoFlush = true) =>
            new StreamPipeSource(stream, autoFlush);
    }

    public static partial class Pipe
    {
        private class InMemoryPipeSource : PipeSource
        {
            private readonly byte[] _data;

            public InMemoryPipeSource(byte[] data) => _data = data;

            public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
                await destination.WriteAsync(_data, cancellationToken);
        }

        /// <summary>
        /// Creates a pipe source that reads from in-memory data.
        /// </summary>
        public static PipeSource FromBytes(byte[] data) => new InMemoryPipeSource(data);

        /// <summary>
        /// Creates a pipe source that reads from a string.
        /// </summary>
        public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

        /// <summary>
        /// Creates a pipe source that reads from a string.
        /// Uses <see cref="Console.InputEncoding"/> to encode the string into byte stream.
        /// </summary>
        public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);
    }

    public static partial class Pipe
    {
        private class CommandPipeSource : PipeSource
        {
            private readonly Command _command;

            public CommandPipeSource(Command command) => _command = command;

            public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
                // Removing `.Task` here breaks a few tests in release mode on .NET5.
                // See: https://github.com/Tyrrrz/CliWrap/issues/97
                // Likely an issue with ConfigureAwait.Fody, so may potentially get fixed with a future package update.
                await _command.WithStandardOutputPipe(ToStream(destination)).ExecuteAsync(cancellationToken).Task;
        }

        /// <summary>
        /// Creates a pipe source that reads from standard output of a command.
        /// </summary>
        public static PipeSource FromCommand(Command command) => new CommandPipeSource(command);
    }

    public static partial class Pipe
    {
        private class StreamPipeTarget : PipeTarget
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

        /// <summary>
        /// Creates a pipe target that writes to a stream.
        /// </summary>
        public static PipeTarget ToStream(Stream stream, bool autoFlush = true) =>
            new StreamPipeTarget(stream, autoFlush);
    }

    public static partial class Pipe
    {
        private class StringBuilderPipeTarget : PipeTarget
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
                while ((charsRead = await reader.ReadAsync(buffer.Array, cancellationToken)) > 0)
                {
                    _stringBuilder.Append(buffer.Array, 0, charsRead);
                }
            }
        }

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
    }

    public static partial class Pipe
    {
        private class DelegatePipeTarget : PipeTarget
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

        private class AsyncDelegatePipeTarget : PipeTarget
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

        /// <summary>
        /// Creates a pipe target that invokes a delegate every time a new line is written.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
            new DelegatePipeTarget(handleLine, encoding);

        /// <summary>
        /// Creates a pipe target that invokes a delegate every time a new line is written.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine) =>
            ToDelegate(handleLine, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target that invokes an asynchronous delegate every time a new line is written.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
            new AsyncDelegatePipeTarget(handleLineAsync, encoding);

        /// <summary>
        /// Creates a pipe target that invokes an asynchronous delegate every time a new line is written.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
            ToDelegate(handleLineAsync, Console.OutputEncoding);
    }

    public static partial class Pipe
    {
        private class MergedPipeTarget : PipeTarget
        {
            public IReadOnlyList<PipeTarget> Targets { get; }

            public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => Targets = targets;

            public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
            {
                // Create separate half-duplex sub-streams for each target
                var subStreams = Targets
                    .Select(_ => new HalfDuplexStream())
                    .ToArray();

                try
                {
                    // Start piping from those streams
                    var targetTasks = Targets
                        .Zip(subStreams, async (target, subStream) => await target.CopyFromAsync(subStream, cancellationToken))
                        .ToArray();

                    // Read from master stream and write data to sub-streams
                    using var buffer = PooledBuffer.ForStream();
                    int bytesRead;
                    while ((bytesRead = await source.ReadAsync(buffer.Array, cancellationToken)) > 0)
                    {
                        foreach (var subStream in subStreams)
                            await subStream.WriteAsync(buffer.Array, 0, bytesRead, cancellationToken);
                    }

                    // Report that transmission is complete
                    foreach (var subStream in subStreams)
                        await subStream.ReportCompletionAsync(cancellationToken);

                    // Wait until all tasks complete so that it's safe to dispose streams
                    await Task.WhenAll(targetTasks);
                }
                finally
                {
                    // Cleanup
                    foreach (var subStream in subStreams)
                        await subStream.DisposeAsync();
                }
            }
        }

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
            result.RemoveAll(t => t == NullPipeTarget);

            return result;
        }

        /// <summary>
        /// Creates a pipe target that replicates data across all inner targets.
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
                return ToNull();

            return new MergedPipeTarget(optimizedTargets);
        }

        /// <summary>
        /// Creates a pipe target that replicates data across all inner targets.
        /// </summary>
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);
    }
}