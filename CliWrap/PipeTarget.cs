﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal;

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
        /// Creates a pipe target from a writeable stream.
        /// </summary>
        public static PipeTarget ToStream(Stream stream) => new StreamPipeTarget(stream);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder, Encoding encoding) =>
            new StringBuilderPipeTarget(stringBuilder, encoding);

        /// <summary>
        /// Creates a pipe target from a string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToStringBuilder(StringBuilder stringBuilder) =>
            ToStringBuilder(stringBuilder, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine, Encoding encoding) =>
            new DelegatePipeTarget(handleLine, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Action<string> handleLine) =>
            ToDelegate(handleLine, Console.OutputEncoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync, Encoding encoding) =>
            new AsyncDelegatePipeTarget(handleLineAsync, encoding);

        /// <summary>
        /// Creates a pipe target from a delegate that asynchronously handles the content on a line-by-line basis.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the string from byte stream.
        /// </summary>
        public static PipeTarget ToDelegate(Func<string, Task> handleLineAsync) =>
            ToDelegate(handleLineAsync, Console.OutputEncoding);

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        public static PipeTarget Merge(IEnumerable<PipeTarget> targets)
        {
            var actualTargets = targets.Where(t => t != Null).ToArray();

            if (actualTargets.Length == 1)
                return actualTargets.Single();

            if (actualTargets.Length == 0)
                return Null;

            return new MergedPipeTarget(actualTargets);
        }

        /// <summary>
        /// Merges multiple pipe targets into a single one.
        /// Data pushed to this pipe will be replicated for all inner targets.
        /// </summary>
        public static PipeTarget Merge(params PipeTarget[] targets) => Merge((IEnumerable<PipeTarget>) targets);

        /// <summary>
        /// Pipe target that ignores all data.
        /// </summary>
        public static PipeTarget Null { get; } = ToStream(Stream.Null);
    }

    internal class StreamPipeTarget : PipeTarget
    {
        private readonly Stream _stream;

        public StreamPipeTarget(Stream stream) => _stream = stream;

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) =>
            await source.CopyToAsync(_stream, cancellationToken);
    }

    internal class StringBuilderPipeTarget : PipeTarget
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

            var buffer = new char[BufferSizes.StreamReader];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
            {
                _stringBuilder.Append(buffer, 0, charsRead);
            }
        }
    }

    internal class DelegatePipeTarget : PipeTarget
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

    internal class AsyncDelegatePipeTarget : PipeTarget
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

    internal class MergedPipeTarget : PipeTarget
    {
        private readonly IReadOnlyList<PipeTarget> _targets;

        public MergedPipeTarget(IReadOnlyList<PipeTarget> targets) => _targets = targets;

        public override async Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[BufferSizes.Stream];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                using var bufferStream = new MemoryStream(buffer, 0, bytesRead, false);

                foreach (var target in _targets)
                {
                    bufferStream.Seek(0, SeekOrigin.Begin);
                    await target.CopyFromAsync(bufferStream, cancellationToken);
                }
            }
        }
    }
}