using System.Diagnostics;

namespace CliWrap;

/// <summary>
/// Resource policy assigned to a process.
/// </summary>
public partial class ResourcePolicy(
    ProcessPriorityClass priority = ProcessPriorityClass.Normal,
    nint? affinity = null,
    nint? minWorkingSet = null,
    nint? maxWorkingSet = null
)
{
    /// <summary>
    /// Priority class of the process.
    /// </summary>
    public ProcessPriorityClass Priority { get; } = priority;

    /// <summary>
    /// Processor core affinity mask of the process.
    /// </summary>
    public nint? Affinity { get; } = affinity;

    /// <summary>
    /// Minimum working set size of the process.
    /// </summary>
    public nint? MinWorkingSet { get; } = minWorkingSet;

    /// <summary>
    /// Maximum working set size of the process.
    /// </summary>
    public nint? MaxWorkingSet { get; } = maxWorkingSet;
}

public partial class ResourcePolicy
{
    /// <summary>
    /// Default resource policy.
    /// </summary>
    public static ResourcePolicy Default { get; } = new();
}
