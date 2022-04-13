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
    /// Copies the binary content pushed to the pipe into the destination stream.
    /// </summary>
    public abstract Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default);
}

public partial class PipeSource
{
    /// <summary>
    /// Pipe source that does not provide any data.
    /// Logical equivalent to <code>/dev/null</code>.
    /// </summary>
    public static PipeSource Null { get; } = new NullPipeSource();

    /// <summary>
    /// Creates a pipe source that reads from a stream.
    /// </summary>
    public static PipeSource FromStream(Stream stream, bool autoFlush) => new StreamPipeSource(stream, autoFlush);

    /// <summary>
    /// Creates a pipe source that reads from a stream.
    /// </summary>
    // TODO: (breaking change) remove in favor of optional parameter
    public static PipeSource FromStream(Stream stream) => FromStream(stream, true);

    /// <summary>
    /// Creates a pipe source that reads from a file.
    /// </summary>
    public static PipeSource FromFile(string filePath) => new FilePipeSource(filePath);

    /// <summary>
    /// Creates a pipe source that reads from memory.
    /// </summary>
    public static PipeSource FromMemory(ReadOnlyMemory<byte> data) => new MemoryPipeSource(data);

    /// <summary>
    /// Creates a pipe source that reads from a byte array.
    /// </summary>
    public static PipeSource FromBytes(byte[] data) => FromMemory(data);

    /// <summary>
    /// Creates a pipe source that reads from a string.
    /// </summary>
    public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

    /// <summary>
    /// Creates a pipe source that reads from a string.
    /// Uses <see cref="Console.InputEncoding"/> to encode the string.
    /// </summary>
    public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);

    /// <summary>
    /// Creates a pipe source that reads from the standard output of a command.
    /// </summary>
    public static PipeSource FromCommand(Command command) => new CommandPipeSource(command);

    /// <summary>
    /// Creates a pipe source that reads from a synchronous delegate that writes to a <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="source">The synchronous delegate that writes to a <see cref="TextWriter"/>.</param>
    /// <param name="options">The optional <see cref="DelegatePipeSourceOptions"/> controlling how the underlying <see cref="StreamWriter"/> is created.</param>
    public static PipeSource FromDelegate(Action<TextWriter> source, DelegatePipeSourceOptions? options = null) => new DelegatePipeSource(source, options);

    /// <summary>
    /// Creates a pipe source that reads from an asynchronous delegate that writes to a <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="source">The asynchronous delegate that writes to a <see cref="TextWriter"/>.</param>
    /// <param name="options">The optional <see cref="DelegatePipeSourceOptions"/> controlling how the underlying <see cref="StreamWriter"/> is created.</param>
    public static PipeSource FromDelegate(Func<TextWriter, Task> source, DelegatePipeSourceOptions? options = null) => new DelegatePipeSource(source, options);
}

internal class NullPipeSource : PipeSource
{
    public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        !cancellationToken.IsCancellationRequested
            ? Task.CompletedTask
            : Task.FromCanceled(cancellationToken);
}

internal class StreamPipeSource : PipeSource
{
    private readonly Stream _stream;
    private readonly bool _autoFlush;

    public StreamPipeSource(Stream stream, bool autoFlush)
    {
        _stream = stream;
        _autoFlush = autoFlush;
    }

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        await _stream.CopyToAsync(destination, _autoFlush, cancellationToken).ConfigureAwait(false);
}

internal class FilePipeSource : PipeSource
{
    private readonly string _filePath;

    public FilePipeSource(string filePath) => _filePath = filePath;

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var stream = File.OpenRead(_filePath);

        await using (stream.WithAsyncDisposableAdapter())
        {
            await stream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal class MemoryPipeSource : PipeSource
{
    private readonly ReadOnlyMemory<byte> _data;

    public MemoryPipeSource(ReadOnlyMemory<byte> data) => _data = data;

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        await destination.WriteAsync(_data, cancellationToken).ConfigureAwait(false);
}

internal class CommandPipeSource : PipeSource
{
    private readonly Command _command;

    public CommandPipeSource(Command command) => _command = command;

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        await _command
            .WithStandardOutputPipe(PipeTarget.ToStream(destination))
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>
/// Options to configure how the underlying <see cref="StreamWriter"/> is created.
/// </summary>
public class DelegatePipeSourceOptions
{
    /// <summary>
    /// The character encoding used by the underlying <see cref="StreamWriter"/>.
    /// </summary>
#if NETSTANDARD || NETFRAMEWORK
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
#else
    public Encoding? Encoding { get; init; } = null;
#endif

    /// <summary>
    /// The buffer size of the underlying <see cref="StreamWriter"/>, in bytes.
    /// </summary>
    public int BufferSize { get; init; } = -1;

    /// <summary>
    /// Whether the underlying <see cref="StreamWriter"/> will flush its buffer to the underlying stream after every call to <see cref="StreamWriter.Write(char)"/>.
    /// See <see cref="StreamWriter.AutoFlush"/>.
    /// <para/>
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool AutoFlush { get; init; } = false;
}

internal class DelegatePipeSource : PipeSource
{
    private readonly Func<TextWriter, Task>? _asyncSource;
    private readonly Action<TextWriter>? _syncSource;
    private readonly DelegatePipeSourceOptions _options;

    public DelegatePipeSource(Func<TextWriter, Task> source, DelegatePipeSourceOptions? options = null)
    {
        _asyncSource = source ?? throw new ArgumentNullException(nameof(source));
        _options = options ?? new DelegatePipeSourceOptions();
    }

    public DelegatePipeSource(Action<TextWriter> source, DelegatePipeSourceOptions? options = null)
    {
        _syncSource = source ?? throw new ArgumentNullException(nameof(source));
        _options = options ?? new DelegatePipeSourceOptions();
    }

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        await
#endif
        using var streamWriter = new StreamWriter(destination, _options.Encoding, _options.BufferSize, leaveOpen: true);
        streamWriter.AutoFlush = _options.AutoFlush;

        if (_asyncSource is not null)
        {
            await _asyncSource(streamWriter);
        }
        else
        {
            _syncSource!(streamWriter);
        }
    }
}