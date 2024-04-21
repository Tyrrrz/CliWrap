using System;

namespace CliWrap;

/// <summary>
/// Strategy used for veryfing the end of command exectuion.
/// </summary>
[Flags]
public enum CommandExitCondition
{
    /// <summary>
    /// Command is finished when process is finished and all pipes are closed.
    /// </summary>
    PipesClosed = 0,

    /// <summary>
    /// Command is finished when the main process exits,
    /// even if they are child processes still running, which are reusing the same output/error streams.
    /// </summary>
    ProcessExited = 1
}
