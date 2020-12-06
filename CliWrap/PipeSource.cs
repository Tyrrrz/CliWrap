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
        /// Pipe source that pushes no data.
        /// </summary>
        [Obsolete("Use Pipe.FromNull() instead.")]
        public static PipeSource Null { get; } = Pipe.FromNull();

        /// <summary>
        /// Creates a pipe source from a readable stream.
        /// </summary>
        [Obsolete("Use Pipe.FromStream(...) instead.")]
        public static PipeSource FromStream(Stream stream, bool autoFlush) => Pipe.FromStream(stream, autoFlush);

        /// <summary>
        /// Creates a pipe source from a readable stream.
        /// </summary>
        [Obsolete("Use Pipe.FromStream(...) instead.")]
        public static PipeSource FromStream(Stream stream) => FromStream(stream, true);

        /// <summary>
        /// Creates a pipe source from in-memory data.
        /// </summary>
        [Obsolete("Use Pipe.FromBytes(...) instead.")]
        public static PipeSource FromBytes(byte[] data) => Pipe.FromBytes(data);

        /// <summary>
        /// Creates a pipe source from a string.
        /// </summary>
        [Obsolete("Use Pipe.FromString(...) instead.")]
        public static PipeSource FromString(string str, Encoding encoding) => FromBytes(encoding.GetBytes(str));

        /// <summary>
        /// Creates a pipe source from a string.
        /// Uses <see cref="Console.InputEncoding"/> to encode the string into byte stream.
        /// </summary>
        [Obsolete("Use Pipe.FromString(...) instead.")]
        public static PipeSource FromString(string str) => FromString(str, Console.InputEncoding);

        /// <summary>
        /// Creates a pipe source from the standard output of a command.
        /// </summary>
        [Obsolete("Use Pipe.FromCommand(...) instead.")]
        public static PipeSource FromCommand(Command command) => Pipe.FromCommand(command);
    }




}