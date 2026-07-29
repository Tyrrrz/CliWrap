using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Utils;
using PowerKit.Extensions;

namespace CliWrap;

/// <summary>
/// Represents a pipe for the process's standard input stream.
/// </summary>
public abstract partial class PipeSource
{
    /// <summary>
    /// Reads the binary content pushed into the pipe and writes it to the destination stream.
    /// Destination stream represents the process's standard input stream.
    /// </summary>
    public abstract Task CopyToAsync(
        Stream destination,
        CancellationToken cancellationToken = default
    );
}

public partial class PipeSource
{
    private class AnonymousPipeSource(Func<Stream, CancellationToken, Task> copyToAsync)
        : PipeSource
    {
        public override async Task CopyToAsync(
            Stream destination,
            CancellationToken cancellationToken = default
        ) => await copyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}

public partial class PipeSource
{
    /// <summary>
    /// Pipe source that does not provide any data.
    /// Functionally equivalent to a null device.
    /// </summary>
    public static PipeSource Null { get; } =
        Create(
            (_, cancellationToken) =>
                !cancellationToken.IsCancellationRequested
                    ? Task.CompletedTask
                    : Task.FromCanceled(cancellationToken)
        );

    /// <summary>
    /// Creates an anonymous pipe source with the <see cref="CopyToAsync(Stream, CancellationToken)" /> method
    /// implemented by the specified asynchronous delegate.
    /// </summary>
    public static PipeSource Create(Func<Stream, CancellationToken, Task> copyToAsync) =>
        new AnonymousPipeSource(copyToAsync);

    /// <summary>
    /// Creates an anonymous pipe source with the <see cref="CopyToAsync(Stream, CancellationToken)" /> method
    /// implemented by the specified synchronous delegate.
    /// </summary>
    public static PipeSource Create(Action<Stream> copyTo) =>
        Create(
            (destination, _) =>
            {
                copyTo(destination);
                return Task.CompletedTask;
            }
        );

    /// <summary>
    /// Creates a pipe source that reads from the specified stream.
    /// </summary>
    public static PipeSource FromStream(Stream stream, bool autoFlush) =>
        Create(
            async (destination, cancellationToken) =>
                await stream
                    .CopyToAsync(destination, autoFlush, cancellationToken)
                    .ConfigureAwait(false)
        );

    /// <inheritdoc cref="FromStream(Stream, bool)" />
    // TODO: (breaking change) remove in favor of optional parameter
    public static PipeSource FromStream(Stream stream) => FromStream(stream, true);

    /// <summary>
    /// Creates a pipe source that reads from the specified file.
    /// </summary>
    public static PipeSource FromFile(string filePath) =>
        Create(
            async (destination, cancellationToken) =>
            {
                var file = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSizes.Stream,
                    FileOptions.Asynchronous | FileOptions.SequentialScan
                );

                await using (file.ToAsyncDisposable())
                    await file.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }
        );

    /// <summary>
    /// Creates a pipe source that reads from the specified memory buffer.
    /// </summary>
    public static PipeSource FromBytes(ReadOnlyMemory<byte> data) =>
        Create(
            async (destination, cancellationToken) =>
                await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false)
        );

    /// <inheritdoc cref="FromBytes(ReadOnlyMemory{byte})" />
    public static PipeSource FromBytes(byte[] data) => FromBytes((ReadOnlyMemory<byte>)data);

    /// <inheritdoc cref="FromBytes(ReadOnlyMemory{byte})" />
    [Obsolete("Use FromBytes(ReadOnlyMemory<byte>) instead"), ExcludeFromCodeCoverage]
    public static PipeSource FromMemory(ReadOnlyMemory<byte> data) => FromBytes(data);

    /// <summary>
    /// Creates a pipe source that reads from the specified string.
    /// </summary>
    public static PipeSource FromString(string data, Encoding encoding) =>
        FromBytes(encoding.GetBytes(data));

    /// <inheritdoc cref="FromString(string, Encoding)" />
    public static PipeSource FromString(string data) => FromString(data, Console.InputEncoding);

    /// <summary>
    /// Creates a pipe source that reads from the standard output of the specified command.
    /// </summary>
    public static PipeSource FromCommand(
        Command command,
        Func<Stream, Stream, CancellationToken, Task> copyStreamAsync
    ) =>
        // cmdA | <transform> | cmdB
        Create(
            // Destination = cmdB's standard input
            async (destination, destinationCancellationToken) =>
                await command
                    .WithStandardOutputPipe(
                        PipeTarget.Create(
                            // Source = cmdA's standard output
                            async (source, sourceCancellationToken) =>
                                await copyStreamAsync(source, destination, sourceCancellationToken)
                                    .ConfigureAwait(false)
                        )
                    )
                    .ExecuteAsync(destinationCancellationToken)
                    .ConfigureAwait(false)
        );

    /// <inheritdoc cref="FromCommand(Command, Func{Stream, Stream, CancellationToken, Task})" />
    public static PipeSource FromCommand(Command command) =>
        FromCommand(
            command,
            async (source, destination, cancellationToken) =>
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false)
        );
}
