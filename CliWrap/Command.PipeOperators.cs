using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CliWrap;

public partial class Command
{
    /// <summary>
    /// Creates a new command that pipes its standard output to the specified target.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, PipeTarget target) =>
        source.WithStandardOutputPipe(target);

    /// <summary>
    /// Creates a new command that pipes its standard output to a stream.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Stream target) =>
        source | PipeTarget.ToStream(target);

    /// <summary>
    /// Creates a new command that pipes its standard output to a string builder.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, StringBuilder target) =>
        source | PipeTarget.ToStringBuilder(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to a delegate.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Action<string> target) =>
        source | PipeTarget.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to a delegate.
    /// Uses <see cref="Console.OutputEncoding"/> to decode the byte stream.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Func<string, Task> target) =>
        source | PipeTarget.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to separate targets.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (PipeTarget, PipeTarget) targets) =>
        source
            .WithStandardOutputPipe(targets.Item1)
            .WithStandardErrorPipe(targets.Item2);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to separate streams.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (Stream, Stream) targets) =>
        source | (PipeTarget.ToStream(targets.Item1), PipeTarget.ToStream(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to separate string builders.
    /// Uses <see cref="Console.OutputEncoding"/> to decode byte streams.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (StringBuilder, StringBuilder) targets) =>
        source | (PipeTarget.ToStringBuilder(targets.Item1), PipeTarget.ToStringBuilder(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line to separate delegates.
    /// Uses <see cref="Console.OutputEncoding"/> to decode byte streams.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (Action<string>, Action<string>) targets) =>
        source | (PipeTarget.ToDelegate(targets.Item1), PipeTarget.ToDelegate(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line to separate delegates.
    /// Uses <see cref="Console.OutputEncoding"/> to decode byte streams.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (Func<string, Task>, Func<string, Task>) targets) =>
        source | (PipeTarget.ToDelegate(targets.Item1), PipeTarget.ToDelegate(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified source.
    /// </summary>
    [Pure]
    public static Command operator |(PipeSource source, Command target) =>
        target.WithStandardInputPipe(source);

    /// <summary>
    /// Creates a new command that pipes its standard input from a stream.
    /// </summary>
    [Pure]
    public static Command operator |(Stream source, Command target) =>
        PipeSource.FromStream(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from memory.
    /// </summary>
    [Pure]
    public static Command operator |(ReadOnlyMemory<byte> source, Command target) =>
        PipeSource.FromMemory(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from a byte array.
    /// </summary>
    [Pure]
    public static Command operator |(byte[] source, Command target) =>
        PipeSource.FromBytes(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from a string.
    /// Uses <see cref="Console.InputEncoding"/> to encode the string.
    /// </summary>
    [Pure]
    public static Command operator |(string source, Command target) =>
        PipeSource.FromString(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the standard output of another command.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Command target) =>
        PipeSource.FromCommand(source) | target;
}