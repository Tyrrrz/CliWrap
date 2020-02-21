using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap
{
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
        /// Creates a pipe source from a readable stream.
        /// </summary>
        public static PipeSource FromStream(Stream stream) => new StreamPipeSource(stream);

        /// <summary>
        /// Creates a pipe source from in-memory data.
        /// </summary>
        public static PipeSource FromBytes(byte[] data) => new InMemoryPipeSource(data);

        /// <summary>
        /// Creates a pipe source from a string.
        /// </summary>
        public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

        /// <summary>
        /// Creates a pipe source from a string.
        /// Uses <see cref="Console.InputEncoding"/> to encode the string into byte stream.
        /// </summary>
        public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);

        /// <summary>
        /// Creates a pipe source from the standard output of a command.
        /// </summary>
        public static PipeSource FromCommand(Command command) => new CommandPipeSource(command);

        /// <summary>
        /// Pipe source that pushes no data.
        /// </summary>
        public static PipeSource Null { get; } = FromStream(Stream.Null);
    }

    internal class StreamPipeSource : PipeSource
    {
        private readonly Stream _stream;

        public StreamPipeSource(Stream stream) => _stream = stream;

        public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            _stream.CopyToAsync(destination, cancellationToken);
    }

    internal class InMemoryPipeSource : PipeSource
    {
        private readonly byte[] _data;

        public InMemoryPipeSource(byte[] data)
        {
            _data = data;
        }

        public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            destination.WriteAsync(_data, 0, _data.Length, cancellationToken);
    }

    internal class CommandPipeSource : PipeSource
    {
        private readonly Command _command;

        public CommandPipeSource(Command command) => _command = command;

        public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) =>
            _command.WithStandardOutputPipe(PipeTarget.ToStream(destination)).ExecuteAsync(cancellationToken);
    }
}