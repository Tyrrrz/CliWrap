using System.Diagnostics;

namespace CliWrap;

/// <summary>
/// Resource policy assigned to a process.
/// </summary>
/// <remarks>
/// For information on platform support, see attributes on <see cref="Process.PriorityClass" />,
/// <see cref="Process.ProcessorAffinity" />, <see cref="Process.MinWorkingSet" /> and
/// <see cref="Process.MaxWorkingSet" />.
/// </remarks>
public partial class ResourcePolicy(
    ProcessPriorityClass? priority = null,
    nint? affinity = null,
    nint? minWorkingSet = null,
    nint? maxWorkingSet = null
)
{
    /// <summary>
    /// Priority class of the process.
    /// </summary>
    public ProcessPriorityClass? Priority { get; } = priority;

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
