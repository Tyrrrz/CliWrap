using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CliWrap;

public partial class Command
{
    /// <summary>
    /// Creates a new command that pipes its standard output to the specified target.
    /// </summary>
    public static Command operator |(Command source, IPipeTarget target) =>
        source.WithStandardOutputPipe(target);

    /// <summary>
    /// Creates a new command that pipes its standard output to the specified stream.
    /// </summary>
    public static Command operator |(Command source, Stream target) =>
        source | Pipe.ToStream(target);

    /// <summary>
    /// Creates a new command that pipes its standard output to the specified string builder.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, StringBuilder target) =>
        source | Pipe.ToStringBuilder(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, Action<string> target) =>
        source | Pipe.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to the specified delegate.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, Func<string, Task> target) =>
        source | Pipe.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the specified targets.
    /// </summary>
    public static Command operator |(Command source, ValueTuple<IPipeTarget, IPipeTarget> target) =>
        source
            .WithStandardOutputPipe(target.Item1)
            .WithStandardErrorPipe(target.Item2);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the specified streams.
    /// </summary>
    public static Command operator |(Command source, ValueTuple<Stream, Stream> target) =>
        source | (Pipe.ToStream(target.Item1), Pipe.ToStream(target.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the specified string builders.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, ValueTuple<StringBuilder, StringBuilder> target) =>
        source | (Pipe.ToStringBuilder(target.Item1), Pipe.ToStringBuilder(target.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, ValueTuple<Action<string>, Action<string>> target) =>
        source | (Pipe.ToDelegate(target.Item1), Pipe.ToDelegate(target.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line to the specified delegates.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    public static Command operator |(Command source, ValueTuple<Func<string, Task>, Func<string, Task>> target) =>
        source | (Pipe.ToDelegate(target.Item1), Pipe.ToDelegate(target.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified source.
    /// </summary>
    public static Command operator |(IPipeSource source, Command target) =>
        target.WithStandardInputPipe(source);

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified stream.
    /// </summary>
    public static Command operator |(Stream source, Command target) =>
        Pipe.FromStream(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified byte array.
    /// </summary>
    public static Command operator |(byte[] source, Command target) =>
        Pipe.FromBytes(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified string.
    /// Uses <see cref="Console.InputEncoding"/> to encode the string.
    /// </summary>
    public static Command operator |(string source, Command target) =>
        Pipe.FromString(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the standard output of the specified other command.
    /// </summary>
    public static Command operator |(Command source, Command target) =>
        Pipe.FromCommand(source) | target;
}