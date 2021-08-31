using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CliWrap
{
    public partial class Command
    {
        /// <summary>
        /// Creates a new command that pipes its standard output to the specified target.
        /// </summary>
        public static Command operator |(Command source, PipeTarget target) =>
            source.WithStandardOutputPipe(target);

        /// <summary>
        /// Creates a new command that pipes its standard output to the specified stream.
        /// </summary>
        public static Command operator |(Command source, Stream target) =>
            source | PipeTarget.ToStream(target);

        /// <summary>
        /// Creates a new command that pipes its standard output to the specified string builder.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, StringBuilder target) =>
            source | PipeTarget.ToStringBuilder(target);

        /// <summary>
        /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, Action<string> target) =>
            source | PipeTarget.ToDelegate(target);

        /// <summary>
        /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, Func<string, Task> target) =>
            source | PipeTarget.ToDelegate(target);

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified targets.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<PipeTarget, PipeTarget> target) =>
            source
                .WithStandardOutputPipe(target.Item1)
                .WithStandardErrorPipe(target.Item2);

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified streams.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Stream, Stream> target) =>
            source | (PipeTarget.ToStream(target.Item1), PipeTarget.ToStream(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error to the specified string builders.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<StringBuilder, StringBuilder> target) =>
            source | (PipeTarget.ToStringBuilder(target.Item1), PipeTarget.ToStringBuilder(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Action<string>, Action<string>> target) =>
            source | (PipeTarget.ToDelegate(target.Item1), PipeTarget.ToDelegate(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
        /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
        /// </summary>
        public static Command operator |(Command source, ValueTuple<Func<string, Task>, Func<string, Task>> target) =>
            source | (PipeTarget.ToDelegate(target.Item1), PipeTarget.ToDelegate(target.Item2));

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified source.
        /// </summary>
        public static Command operator |(PipeSource source, Command target) =>
            target.WithStandardInputPipe(source);

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified stream.
        /// </summary>
        public static Command operator |(Stream source, Command target) =>
            PipeSource.FromStream(source) | target;

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified byte array.
        /// </summary>
        public static Command operator |(byte[] source, Command target) =>
            PipeSource.FromBytes(source) | target;

        /// <summary>
        /// Creates a new command that pipes its standard input from the specified string.
        /// Uses <see cref="Console.InputEncoding"/> to encode the string.
        /// </summary>
        public static Command operator |(string source, Command target) =>
            PipeSource.FromString(source) | target;

        /// <summary>
        /// Creates a new command that pipes its standard input from the standard output of the specified other command.
        /// </summary>
        public static Command operator |(Command source, Command target) =>
            PipeSource.FromCommand(source) | target;
    }
}