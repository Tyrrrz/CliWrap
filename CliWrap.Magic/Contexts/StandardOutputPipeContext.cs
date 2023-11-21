using System;
using Contextual;

namespace CliWrap.Magic.Contexts;

internal class StandardOutputPipeContext : Context
{
    public PipeTarget Pipe { get; }

    public StandardOutputPipeContext(PipeTarget pipe) => Pipe = pipe;

    public StandardOutputPipeContext()
        : this(PipeTarget.ToStream(Console.OpenStandardOutput())) { }
}
