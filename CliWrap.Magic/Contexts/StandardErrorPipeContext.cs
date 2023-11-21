using System;
using Contextual;

namespace CliWrap.Magic.Contexts;

internal class StandardErrorPipeContext : Context
{
    public PipeTarget Pipe { get; }

    public StandardErrorPipeContext(PipeTarget pipe) => Pipe = pipe;

    public StandardErrorPipeContext()
        : this(PipeTarget.ToStream(Console.OpenStandardError())) { }
}
