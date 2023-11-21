using System;
using Contextual;

namespace CliWrap.Magic.Contexts;

internal class StandardInputPipeContext : Context
{
    public PipeSource Pipe { get; }

    public StandardInputPipeContext(PipeSource pipe) => Pipe = pipe;

    public StandardInputPipeContext()
        : this(PipeSource.FromStream(Console.OpenStandardInput())) { }
}
