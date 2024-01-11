using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CliWrap.Utils.Extensions;

namespace CliWrap;

/// <summary>
/// Represents an asynchronous execution of a command.
/// </summary>
public partial class CommandTask<TResult>(Task<TResult> task, int processId) : IDisposable
{
    /// <summary>
    /// Underlying task.
    /// </summary>
    public Task<TResult> Task { get; } = task;

    /// <summary>
    /// Underlying process ID.
    /// </summary>
    public int ProcessId { get; } = processId;

    internal CommandTask<T> Bind<T>(Func<Task<TResult>, Task<T>> transform) =>
        new(transform(Task), ProcessId);

    /// <summary>
    /// Lazily maps the result of the task using the specified transform.
    /// </summary>
    // TODO: (breaking change) this should not be publicly exposed
    public CommandTask<T> Select<T>(Func<TResult, T> transform) =>
        Bind(task => task.Select(transform));

    /// <summary>
    /// Gets the awaiter of the underlying task.
    /// Used to enable await expressions on this object.
    /// </summary>
    public TaskAwaiter<TResult> GetAwaiter() => Task.GetAwaiter();

    /// <summary>
    /// Configures an awaiter used to await this task.
    /// </summary>
    public ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
        Task.ConfigureAwait(continueOnCapturedContext);

    /// <inheritdoc />
    public void Dispose() => Task.Dispose();
}

public partial class CommandTask<TResult>
{
    /// <summary>
    /// Converts the command task into a regular task.
    /// </summary>
    public static implicit operator Task<TResult>(CommandTask<TResult> commandTask) =>
        commandTask.Task;
}
