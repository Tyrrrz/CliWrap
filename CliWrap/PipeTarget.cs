using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// Pipe target that ignores all data.
        /// </summary>
        [Obsolete("Use Pipe.ToNull() instead.")]
        public static PipeTarget Null => Pipe.ToNull();

        /// <summary>
        /// Creates a pipe target from a writeable stream.
        /// </summary>
        [Obsolete("Use Pipe.ToStream(...) instead.")]
        public static PipeTarget ToStream(Stream stream, bool autoFlush) => Pipe.ToStream(stream, autoFlush);

        /// <summary>
        /// Creates a pipe target from a writeable stream.
        /// </summary>
        [Obsolete("Use Pipe.ToStream(...) instead.")]
        public static PipeTarget ToStream(Stream stream) => ToStream(stream, true);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// </summary>
        [Obsolete("Use Pipe.ToStringBuilder(...) instead.")]
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            Pipe.ToStringBuilder(stringBuilder, encoding);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        [Obsolete("Use Pipe.ToStringBuilder(...) instead.")]
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// </summary>
        [Obsolete("Use Pipe.ToDelegate(...) instead.")]
        public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
            Pipe.ToDelegate(handleLine, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        [Obsolete("Use Pipe.ToDelegate(...) instead.")]
        public static PipeTarget ToDelegate(Action<string> handleLine) =>
            ToDelegate(handleLine, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// </summary>
        [Obsolete("Use Pipe.ToDelegate(...) instead.")]
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
            Pipe.ToDelegate(handleLineAsync, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        [Obsolete("Use Pipe.ToDelegate(...) instead.")]
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
            ToDelegate(handleLineAsync, Console.OutputEncoding);

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        [Obsolete("Use Pipe.Merge(...) instead.")]
        public static PipeTarget Merge(IEnumerable<PipeTarget> targets) =>
            Pipe.Merge(targets);

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        [Obsolete("Use Pipe.Merge(...) instead.")]
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);
    }
}