using System.Diagnostics;

namespace CliWrap.Builders;

/// <summary>
/// Builder that helps configure resource policy.
/// </summary>
public class ResourcePolicyBuilder
{
    private ProcessPriorityClass _priority = ProcessPriorityClass.Normal;
    private nint? _affinity;
    private nint? _minWorkingSet;
    private nint? _maxWorkingSet;

    /// <summary>
    /// Sets the priority class of the process.
    /// </summary>
    public ResourcePolicyBuilder SetPriority(ProcessPriorityClass priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the processor core affinity mask of the process.
    /// For example, to set the affinity to cores 1 and 3 out of 4, pass 0b1010.
    /// </summary>
    public ResourcePolicyBuilder SetAffinity(nint? affinity)
    {
        _affinity = affinity;
        return this;
    }

    /// <summary>
    /// Sets the minimum working set size of the process.
    /// </summary>
    public ResourcePolicyBuilder SetMinWorkingSet(nint? minWorkingSet)
    {
        _minWorkingSet = minWorkingSet;
        return this;
    }

    /// <summary>
    /// Sets the maximum working set size of the process.
    /// </summary>
    public ResourcePolicyBuilder SetMaxWorkingSet(nint? maxWorkingSet)
    {
        _maxWorkingSet = maxWorkingSet;
        return this;
    }

    /// <summary>
    /// Builds the resulting resource policy.
    /// </summary>
    public ResourcePolicy Build() => new(_priority, _affinity, _minWorkingSet, _maxWorkingSet);
}
