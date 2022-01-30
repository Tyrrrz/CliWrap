using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils;
using CliWrap.Utils.Extensions;

namespace CliWrap;

/// <summary>
/// Factory methods for creating pipes.
/// </summary>
public static partial class Pipe
{
    private static readonly NullPipeSource NullSource = new();
    private static readonly NullPipeTarget NullTarget = new();

    /// <summary>
    /// Pipe source that does not provide any data.
    /// Logical equivalent to <code>/dev/null</code>.
    /// </summary>
    public static IPipeSource FromNull() => NullSource;

    /// <summary>
    /// Creates a pipe source that reads from a stream.
    /// </summary>
    public static IPipeSource FromStream(Stream stream, bool autoFlush = true) =>
        new StreamPipeSource(stream, autoFlush);

    /// <summary>
    /// Creates a pipe source that reads from a file.
    /// </summary>
    public static IPipeSource FromFile(string filePath) => new FilePipeSource(filePath);

    /// <summary>
    /// Creates a pipe source that reads from a byte array.
    /// </summary>
    public static IPipeSource FromBytes(byte[] data) => new InMemoryPipeSource(data);

    /// <summary>
    /// Creates a pipe source that reads from a string.
    /// </summary>
    public static IPipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

    /// <summary>
    /// Creates a pipe source that reads from a string.
    /// Uses <see cref="Console.InputEncoding"/> to encode the string.
    /// </summary>
    public static IPipeSource FromString(string str) => FromString(str, Console.InputEncoding);

    /// <summary>
    /// Creates a pipe source that reads from standard output of a command.
    /// </summary>
    public static IPipeSource FromCommand(Command command) => new CommandPipeSource(command);

    /// <summary>
    /// Pipe target that discards all data.
    /// Logical equivalent to <code>/dev/null</code>.
    /// </summary>
    public static IPipeTarget ToNull() => NullTarget;

    /// <summary>
    /// Creates a pipe target that writes to a stream.
    /// </summary>
    public static IPipeTarget ToStream(Stream stream, bool autoFlush = true) => new StreamPipeTarget(stream, autoFlush);

    /// <summary>
    /// Creates a pipe target that writes to a file.
    /// </summary>
    public static IPipeTarget ToFile(string filePath) => new FilePipeTarget(filePath);

    /// <summary>
    /// Creates a pipe target that writes to a string builder.
    /// </summary>
    public static IPipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
        new StringBuilderPipeTarget(stringBuilder, encoding);

    /// <summary>
    /// Creates a pipe target that writes to a string builder.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static IPipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
        ToStringBuilder(stringBuilder, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that triggers a delegate on every line written.
    /// </summary>
    public static IPipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
        new DelegatePipeTarget(handleLine, encoding);

    /// <summary>
    /// Creates a pipe target that triggers a delegate on every line written.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static IPipeTarget ToDelegate(Action<string> handleLine) =>
        ToDelegate(handleLine, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that triggers an asynchronous delegate on every line written.
    /// </summary>
    public static IPipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
        new AsyncDelegatePipeTarget(handleLineAsync, encoding);

    /// <summary>
    /// Creates a pipe target that triggers an asynchronous delegate on every line written.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static IPipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
        ToDelegate(handleLineAsync, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that replicates data over multiple inner targets.
    /// </summary>
    public static IPipeTarget ToMany(IEnumerable<IPipeTarget> targets)
    {
        // This function needs to take output as a parameter because it's recursive
        static void FlattenTargets(IEnumerable<IPipeTarget> targets, ICollection<IPipeTarget> output)
        {
            foreach (var target in targets)
            {
                if (target is AggregatePipeTarget aggregateTarget)
                {
                    FlattenTargets(aggregateTarget.Targets, output);
                }
                else
                {
                    output.Add(target);
                }
            }
        }

        static IReadOnlyList<IPipeTarget> OptimizeTargets(IEnumerable<IPipeTarget> targets)
        {
            var result = new List<IPipeTarget>();

            // Unwrap merged targets
            FlattenTargets(targets, result);

            // Filter out no-op
            result.RemoveAll(t => t is NullPipeTarget);

            return result;
        }

        // Optimize targets to avoid unnecessary work
        var optimizedTargets = OptimizeTargets(targets);

        // Avoid merging if there's only one target
        if (optimizedTargets.Count == 1)
            return optimizedTargets.Single();

        // Avoid merging if there are no targets
        if (optimizedTargets.Count == 0)
            return ToNull();

        return new AggregatePipeTarget(optimizedTargets);
    }

    /// <summary>
    /// Creates a pipe target that replicates data over multiple inner targets.
    /// </summary>
    public static IPipeTarget ToMany(params IPipeTarget[] targets) =>
        ToMany((IEnumerable<IPipeTarget>) targets);
}

// Pipe sources
public partial class Pipe
{
    private class NullPipeSource : IPipeSource
    {
        public Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            !cancellationToken.IsCancellationRequested
                ? Task.CompletedTask
                : Task.FromCanceled(cancellationToken);
    }

    private class StreamPipeSource : IPipeSource
    {
        private readonly Stream _stream;
        private readonly bool _autoFlush;

        public StreamPipeSource(Stream stream, bool autoFlush)
        {
            _stream = stream;
            _autoFlush = autoFlush;
        }

        public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            await _stream.CopyToAsync(destination, _autoFlush, cancellationToken).ConfigureAwait(false);
    }

    private class FilePipeSource : IPipeSource
    {
        private readonly string _filePath;

        public FilePipeSource(string filePath) => _filePath = filePath;

        public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
        {
            await using var stream = File.OpenRead(_filePath);
            await stream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
    }

    private class InMemoryPipeSource : IPipeSource
    {
        private readonly byte[] _data;

        public InMemoryPipeSource(byte[] data) => _data = data;

        public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            await destination.WriteAsync(_data, cancellationToken).ConfigureAwait(false);
    }

    private class CommandPipeSource : IPipeSource
    {
        private readonly Command _command;

        public CommandPipeSource(Command command) => _command = command;

        public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            await _command
                .WithStandardOutputPipe(ToStream(destination))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
    }
}

// Pipe targets
public partial class Pipe
{
    private class NullPipeTarget : IPipeTarget
    {
        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            // We need to actually exhaust the input stream to avoid potential deadlocks.
            // TODO: none of the tests fail if this is replaced with Task.CompletedTask,
            // so the above claim may be incorrect. Need to verify.
            await source.CopyToAsync(Stream.Null, cancellationToken).ConfigureAwait(false);
        }
    }

    private class StreamPipeTarget : IPipeTarget
    {
        private readonly Stream _stream;
        private readonly bool _autoFlush;

        public StreamPipeTarget(Stream stream, bool autoFlush)
        {
            _stream = stream;
            _autoFlush = autoFlush;
        }

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            await source.CopyToAsync(_stream, _autoFlush, cancellationToken).ConfigureAwait(false);
    }

    private class FilePipeTarget : IPipeTarget
    {
        private readonly string _filePath;

        public FilePipeTarget(string filePath) => _filePath = filePath;

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            await using var stream = File.Create(_filePath);
            await source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private class StringBuilderPipeTarget : IPipeTarget
    {
        private readonly StringBuilder _stringBuilder;
        private readonly Encoding _encoding;

        public StringBuilderPipeTarget(StringBuilder stringBuilder, Encoding encoding)
        {
            _stringBuilder = stringBuilder;
            _encoding = encoding;
        }

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
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

    private class DelegatePipeTarget : IPipeTarget
    {
        private readonly Action<string> _handle;
        private readonly Encoding _encoding;

        public DelegatePipeTarget(Action<string> handle, Encoding encoding)
        {
            _handle = handle;
            _encoding = encoding;
        }

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(source, _encoding, false, BufferSizes.StreamReader, true);

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                _handle(line);
            }
        }
    }

    private class AsyncDelegatePipeTarget : IPipeTarget
    {
        private readonly Func<string, Task> _handleAsync;
        private readonly Encoding _encoding;

        public AsyncDelegatePipeTarget(Func<string, Task> handleAsync, Encoding encoding)
        {
            _handleAsync = handleAsync;
            _encoding = encoding;
        }

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(source, _encoding, false, BufferSizes.StreamReader, true);

            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                await _handleAsync(line).ConfigureAwait(false);
            }
        }
    }

    private class AggregatePipeTarget : IPipeTarget
    {
        public IReadOnlyList<IPipeTarget> Targets { get; }

        public AggregatePipeTarget(IReadOnlyList<IPipeTarget> targets) => Targets = targets;

        public async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            // Create a separate sub-stream for each target
            var targetSubStreams = new Dictionary<IPipeTarget, SimplexStream>();
            foreach (var target in Targets)
                targetSubStreams[target] = new SimplexStream();

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