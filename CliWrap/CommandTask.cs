using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CliWrap.Utils.Extensions;

namespace CliWrap
{
    /// <summary>
    /// Represents an asynchronous execution of a command.
    /// </summary>
    public partial class CommandTask<TResult> : IDisposable
    {
        /// <summary>
        /// Inner task.
        /// </summary>
        public Task<TResult> Task { get; }

        /// <summary>
        /// Underlying process ID.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Initializes an instance of <see cref="CommandTask{TResult}"/>.
        /// </summary>
        public CommandTask(Task<TResult> task, int processId)
        {
            Task = task;
            ProcessId = processId;
        }

        /// <summary>
        /// Lazily maps the result of the task using the specified transform.
        /// </summary>
        public CommandTask<T> Select<T>(Func<TResult, T> transform) => new(Task.Select(transform), ProcessId);

        /// <summary>
        /// Gets the awaiter of the inner task.
        /// Used to facilitate async/await expressions on this object.
        /// </summary>
        public TaskAwaiter<TResult> GetAwaiter() => Task.GetAwaiter();

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            Task.ConfigureAwait(continueOnCapturedContext);

        /// <summary>
        /// Disposes the inner task.
        /// There is no need to call this manually, unless you are not planning to await the task.
        /// </summary>
        public void Dispose() => Task.Dispose();
    }

    public partial class CommandTask<TResult>
    {
        /// <summary>
        /// Casts a command task into a regular task.
        /// </summary>
        public static implicit operator Task<TResult>(CommandTask<TResult> commandTask) => commandTask.Task;
    }
}