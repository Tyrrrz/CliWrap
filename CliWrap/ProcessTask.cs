using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CliWrap
{
    public class ProcessTask<TResult> : IDisposable
    {
        public Task<TResult> Task { get; }

        public int ProcessId { get; }

        public ProcessTask(Task<TResult> task, int processId)
        {
            Task = task;
            ProcessId = processId;
        }

        public TaskAwaiter<TResult> GetAwaiter() => Task.GetAwaiter();

        public void Dispose() => Task.Dispose();
    }
}