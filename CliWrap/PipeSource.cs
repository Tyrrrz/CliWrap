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
    /// Logical equivalent to /dev/null.
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
    /// Creates a pipe source that reads from a synchronous delegate that writes to a <see cref="Stream"/>.
    /// </summary>
    public static PipeSource Create(Action<Stream> source) => new AnonymousPipeSource(source);

    /// <summary>
    /// Creates a pipe source that reads from an asynchronous delegate that writes to a <see cref="Stream"/>.
    /// </summary>
    public static PipeSource Create(Func<Stream, CancellationToken, Task> source) => new AsyncAnonymousPipeSource(source);

    /// <summary>
    /// Creates a pipe source that reads from a synchronous delegate that writes to a <see cref="TextWriter"/>.
    /// </summary>
    public static PipeSource Create(Action<TextWriter> source, Encoding? encoding = null, int bufferSize = -1, bool autoFlush = false) =>
        new TextWriterAnonymousPipeSource(source, encoding ?? Console.InputEncoding, bufferSize, autoFlush);

    /// <summary>
    /// Creates a pipe source that reads from an asynchronous delegate that writes to a <see cref="TextWriter"/>.
    /// </summary>
    public static PipeSource Create(Func<TextWriter, CancellationToken, Task> source, Encoding? encoding = null, int bufferSize = -1, bool autoFlush = false) =>
        new TextWriterAsyncAnonymousPipeSource(source, encoding ?? Console.InputEncoding, bufferSize, autoFlush);
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

internal class AnonymousPipeSource : PipeSource
{
    private readonly Action<Stream> _source;

    public AnonymousPipeSource(Action<Stream> source) => _source = source;

    public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        _source(destination);
        return Task.CompletedTask;
    }
}

internal class AsyncAnonymousPipeSource : PipeSource
{
    private readonly Func<Stream, CancellationToken, Task> _source;

    public AsyncAnonymousPipeSource(Func<Stream, CancellationToken, Task> source) => _source = source;

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
        await _source(destination, cancellationToken).ConfigureAwait(false);
}

internal class TextWriterAnonymousPipeSource : PipeSource
{
    private readonly Action<TextWriter> _source;
    private readonly Encoding _encoding;
    private readonly int _bufferSize;
    private readonly bool _autoFlush;

    public TextWriterAnonymousPipeSource(Action<TextWriter> source, Encoding encoding, int bufferSize, bool autoFlush)
    {
        _source = source;
        _encoding = encoding;
        _bufferSize = bufferSize;
        _autoFlush = autoFlush;
    }

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var streamWriter = new StreamWriter(destination, _encoding, _bufferSize, leaveOpen: true);
        streamWriter.AutoFlush = _autoFlush;
        await using (streamWriter.WithAsyncDisposableAdapter())
        {
            _source(streamWriter);
        }
    }
}

internal class TextWriterAsyncAnonymousPipeSource : PipeSource
{
    private readonly Func<TextWriter, CancellationToken, Task> _source;
    private readonly Encoding _encoding;
    private readonly int _bufferSize;
    private readonly bool _autoFlush;

    public TextWriterAsyncAnonymousPipeSource(Func<TextWriter, CancellationToken, Task> source, Encoding encoding, int bufferSize, bool autoFlush)
    {
        _source = source;
        _encoding = encoding;
        _bufferSize = bufferSize;
        _autoFlush = autoFlush;
    }

    public override async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var streamWriter = new StreamWriter(destination, _encoding, _bufferSize, leaveOpen: true);
        streamWriter.AutoFlush = _autoFlush;
        await using (streamWriter.WithAsyncDisposableAdapter())
        {
            await _source(streamWriter, cancellationToken).ConfigureAwait(false);
        }
    }
}