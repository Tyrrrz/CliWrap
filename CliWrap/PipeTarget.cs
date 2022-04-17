using System;
using System.Buffers;
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
/// Abstraction that represents an outwards-facing pipe.
/// </summary>
public abstract partial class PipeTarget
{
    /// <summary>
    /// Copies the binary content from the stream and pushes it into the pipe.
    /// </summary>
    public abstract Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default);
}

internal class AnonymousPipeTarget : PipeTarget
{
    private readonly Func<Stream, CancellationToken, Task> _copyFromAsync;

    public AnonymousPipeTarget(Func<Stream, CancellationToken, Task> copyFromAsync) =>
        _copyFromAsync = copyFromAsync;

    public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
        await _copyFromAsync(source, cancellationToken).ConfigureAwait(false);
}

internal class MergedPipeTarget : PipeTarget
{
    public IReadOnlyList<PipeTarget> Targets { get; }

    public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => Targets = targets;

    public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
    {
        // Create a separate sub-stream for each target
        var targetSubStreams = new Dictionary<PipeTarget, SimplexStream>();
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
            using var buffer = MemoryPool<byte>.Shared.Rent(BufferSizes.Stream);
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false)) > 0)
            {
                foreach (var (_, subStream) in targetSubStreams)
                {
                    await subStream.WriteAsync(buffer.Memory[..bytesRead], cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            // Report that transmission is complete
            foreach (var (_, subStream) in targetSubStreams)
                await subStream.ReportCompletionAsync(cancellationToken).ConfigureAwait(false);

            await readingTask.ConfigureAwait(false);
        }
        finally
        {
            foreach (var (_, subStream) in targetSubStreams)
                await subStream.WithAsyncDisposableAdapter().DisposeAsync().ConfigureAwait(false);
        }
    }
}

public partial class PipeTarget
{
    /// <summary>
    /// Pipe target that discards all data.
    /// Logical equivalent to /dev/null.
    /// </summary>
    /// <remarks>
    /// Using this target results in the corresponding stream (stdout or stderr) not being opened for the underlying
    /// process at all. In most cases, this behavior should be functionally equivalent to writing to a null stream,
    /// but without the performance overhad of discarding unneeded data.
    /// This may cause issues in some situations, however, for example if the process attempts to write data to the
    /// destination stream without first checking if it's open.
    /// In such cases, it may be better to use <see cref="ToStream(Stream)" /> with <see cref="Stream.Null" /> instead.
    /// </remarks>
    public static PipeTarget Null { get; } = Create((source, cancellationToken) =>
         !cancellationToken.IsCancellationRequested
            ? Task.CompletedTask
            : Task.FromCanceled(cancellationToken)
    );

    /// <summary>
    /// Creates an anonymous pipe target implemented using the specified delegate.
    /// </summary>
    public static PipeTarget Create(Func<Stream, CancellationToken, Task> handlePipeAsync) =>
        new AnonymousPipeTarget(handlePipeAsync);

    /// <summary>
    /// Creates an anonymous pipe target implemented using the specified delegate.
    /// </summary>
    public static PipeTarget Create(Action<Stream> handlePipe) => Create((source, _) =>
    {
        handlePipe(source);
        return Task.CompletedTask;
    });

    /// <summary>
    /// Creates a pipe target that writes to a stream.
    /// </summary>
    public static PipeTarget ToStream(Stream stream, bool autoFlush) => Create(async (source, cancellationToken) =>
        await source.CopyToAsync(stream, autoFlush, cancellationToken).ConfigureAwait(false)
    );

    /// <summary>
    /// Creates a pipe target that writes to a stream.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public static PipeTarget ToStream(Stream stream) => ToStream(stream, true);

    /// <summary>
    /// Creates a pipe target that writes to a file.
    /// </summary>
    public static PipeTarget ToFile(string filePath) => Create(async (source, cancellationToken) =>
    {
        var target = File.Create(filePath);
        await using (target.WithAsyncDisposableAdapter())
            await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    });

    /// <summary>
    /// Creates a pipe target that writes to a string builder.
    /// </summary>
    public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
        Create(async (source, cancellationToken) =>
        {
            using var reader = new StreamReader(source, encoding, false, BufferSizes.StreamReader, true);
            using var buffer = MemoryPool<char>.Shared.Rent(BufferSizes.StreamReader);

            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer.Memory, cancellationToken).ConfigureAwait(false)) > 0)
                stringBuilder.Append(buffer.Memory[..charsRead]);
        });

    /// <summary>
    /// Creates a pipe target that writes to a string builder.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
        ToStringBuilder(stringBuilder, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that invokes a delegate on every line written.
    /// </summary>
    public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
        Create(async (source, cancellationToken) =>
        {
            using var reader = new StreamReader(source, encoding, false, BufferSizes.StreamReader, true);
            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
                handleLine(line);
        });

    /// <summary>
    /// Creates a pipe target that invokes a delegate on every line written.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static PipeTarget ToDelegate(Action<string> handleLine) =>
        ToDelegate(handleLine, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that invokes an asynchronous delegate on every line written.
    /// </summary>
    public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
        Create(async (source, cancellationToken) =>
        {
            using var reader = new StreamReader(source, encoding, false, BufferSizes.StreamReader, true);
            await foreach (var line in reader.ReadAllLinesAsync(cancellationToken).ConfigureAwait(false))
                await handleLineAsync(line).ConfigureAwait(false);
        });

    /// <summary>
    /// Creates a pipe target that invokes an asynchronous delegate on every line written.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
        ToDelegate(handleLineAsync, Console.OutputEncoding);

    /// <summary>
    /// Creates a pipe target that replicates data over multiple inner targets.
    /// </summary>
    public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
    {
        // This function needs to take output as a parameter because it's recursive
        static void FlattenTargets(IEnumerable<PipeTarget> targets, ICollection<PipeTarget> output)
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

        static IReadOnlyList<PipeTarget> OptimizeTargets(IEnumerable<PipeTarget> targets)
        {
            var result = new List<PipeTarget>();

            // Unwrap merged targets
            FlattenTargets(targets, result);

            // Filter out no-op
            result.RemoveAll(t => t == Null);

            return result;
        }

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
    public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>)targets);
}