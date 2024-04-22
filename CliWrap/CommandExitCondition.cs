namespace CliWrap;

/// <summary>
/// Strategy used for identifying the end of command exectuion.
/// </summary>
public enum CommandExitCondition
{
    /// <summary>
    /// Command execution is considered finished when the process exits and all standard input and output streams are closed.
    /// </summary>
    PipesClosed = 0,

    /// <summary>
    /// Command execution is considered finished when the process exits, even if the process's standard input and output streams are still open,
    /// for example after being inherited by a grandchild process.
    /// </summary>
    ProcessExited = 1
}
