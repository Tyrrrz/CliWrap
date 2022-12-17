using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils.Extensions;

namespace CliWrap;

/// <summary>
/// Abstraction that represents an inwards-facing pipe.
/// </summary>
public abstract partial class PipeSource
{
    /// <summary>
    /// Reads the binary content pushed into the pipe and writes it to the destination stream.
    /// Destination stream represents the process's standard input stream.
    /// </summary>
    public abstract Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default);
}

internal class AnonymousPipeSource : PipeSource
{
    private readonly Func<Stream, CancellationToken, Task> _copyToAsync;

    public AnonymousPipeSource(Func<Stream, CancellationToken, Task> copyToAsync) =>
        _copyToAsync = copyToAsync;

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        await _copyToAsync(destination, cancellationToken).ConfigureAwait(false);
}

public partial class PipeSource
{
    /// <summary>
    /// Pipe source that does not provide any data.
    /// Functionally equivalent to a null device.
    /// </summary>
    public static PipeSource Null { get; } = Create((_, cancellationToken) =>
         !cancellationToken.IsCancellationRequested
            ? Task.CompletedTask
            : Task.FromCanceled(cancellationToken)
    );

    /// <summary>
    /// Creates an anonymous pipe source with the <see cref="CopyToAsync(Stream, CancellationToken)" /> method
    /// implemented by the specified asynchronous delegate.
    /// </summary>
    public static PipeSource Create(Func<Stream, CancellationToken, Task> handlePipeAsync) =>
        new AnonymousPipeSource(handlePipeAsync);

    /// <summary>
    /// Creates an anonymous pipe source with the <see cref="CopyToAsync(Stream, CancellationToken)" /> method
    /// implemented by the specified synchronous delegate.
    /// </summary>
    public static PipeSource Create(Action<Stream> handlePipe) => Create((destination, _) =>
    {
        handlePipe(destination);
        return Task.CompletedTask;
    });

    /// <summary>
    /// Creates a pipe source that reads from the specified stream.
    /// </summary>
    public static PipeSource FromStream(Stream stream, bool autoFlush) =>
        Create(async (destination, cancellationToken) =>
            await stream.CopyToAsync(destination, autoFlush, cancellationToken).ConfigureAwait(false)
        );

    /// <summary>
    /// Creates a pipe source that reads from the specified stream.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public static PipeSource FromStream(Stream stream) => FromStream(stream, true);

    /// <summary>
    /// Creates a pipe source that reads from the specified file.
    /// </summary>
    public static PipeSource FromFile(string filePath) => Create(async (destination, cancellationToken) =>
    {
        var source = File.OpenRead(filePath);
        await using (source.ToAsyncDisposable())
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    });

    /// <summary>
    /// Creates a pipe source that reads from the specified memory region.
    /// </summary>
    public static PipeSource FromMemory(ReadOnlyMemory<byte> data) => Create(async (destination, cancellationToken) =>
        await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false)
    );

    /// <summary>
    /// Creates a pipe source that reads from the specified byte array.
    /// </summary>
    public static PipeSource FromBytes(byte[] data) => FromMemory(data);

    /// <summary>
    /// Creates a pipe source that reads from the specified string.
    /// </summary>
    public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

    /// <summary>
    /// Creates a pipe source that reads from the specified string.
    /// Uses <see cref="Console.InputEncoding" /> for encoding.
    /// </summary>
    public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);

    /// <summary>
    /// Creates a pipe source that reads from the standard output of the specified command.
    /// </summary>
    public static PipeSource FromCommand(Command command) => Create(async (destination, cancellationToken) =>
        await command
            .WithStandardOutputPipe(PipeTarget.ToStream(destination))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false)
    );
}