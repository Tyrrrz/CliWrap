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
    /// Creates a new command that pipes its standard output to the specified stream.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Stream target) =>
        source | PipeTarget.ToStream(target);

    /// <summary>
    /// Creates a new command that pipes its standard output to the specified string builder.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, StringBuilder target) =>
        source | PipeTarget.ToStringBuilder(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to the specified
    /// synchronous delegate.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Action<string> target) =>
        source | PipeTarget.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output line-by-line to the specified
    /// asynchronous delegate.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Func<string, Task> target) =>
        source | PipeTarget.ToDelegate(target);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the
    /// specified targets.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (PipeTarget, PipeTarget) targets) =>
        source
            .WithStandardOutputPipe(targets.Item1)
            .WithStandardErrorPipe(targets.Item2);

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the
    /// specified streams.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (Stream, Stream) targets) =>
        source | (PipeTarget.ToStream(targets.Item1), PipeTarget.ToStream(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error to the
    /// specified string builders.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (StringBuilder, StringBuilder) targets) =>
        source | (PipeTarget.ToStringBuilder(targets.Item1), PipeTarget.ToStringBuilder(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line
    /// to the specified synchronous delegates.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, (Action<string>, Action<string>) targets) =>
        source | (PipeTarget.ToDelegate(targets.Item1), PipeTarget.ToDelegate(targets.Item2));

    /// <summary>
    /// Creates a new command that pipes its standard output and standard error line-by-line
    /// to the specified asynchronous delegates.
    /// Uses <see cref="Console.OutputEncoding" /> for decoding.
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
    /// Creates a new command that pipes its standard input from the specified stream.
    /// </summary>
    [Pure]
    public static Command operator |(Stream source, Command target) =>
        PipeSource.FromStream(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified memory region.
    /// </summary>
    [Pure]
    public static Command operator |(ReadOnlyMemory<byte> source, Command target) =>
        PipeSource.FromMemory(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified byte array.
    /// </summary>
    [Pure]
    public static Command operator |(byte[] source, Command target) =>
        PipeSource.FromBytes(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the specified string.
    /// Uses <see cref="Console.InputEncoding" /> for encoding.
    /// </summary>
    [Pure]
    public static Command operator |(string source, Command target) =>
        PipeSource.FromString(source) | target;

    /// <summary>
    /// Creates a new command that pipes its standard input from the standard output of the
    /// specified command.
    /// </summary>
    [Pure]
    public static Command operator |(Command source, Command target) =>
        PipeSource.FromCommand(source) | target;
}