using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CliWrap
{
    public class CliTask<TResult>
    {
        public Task<TResult> Task { get; }

        public int ProcessId { get; }

        public CliTask(Task<TResult> task, int processId)
        {
            Task = task;
            ProcessId = processId;
        }

        public TaskAwaiter<TResult> GetAwaiter() => Task.GetAwaiter();
    }
}