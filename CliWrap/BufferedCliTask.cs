using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CliWrap
{
    public class BufferedCliTask
    {
        private readonly Task<BufferedCliResult> _task;

        public int ProcessId { get; }

        public BufferedCliTask(Task<BufferedCliResult> task, int processId)
        {
            _task = task;
            ProcessId = processId;
        }

        public TaskAwaiter<BufferedCliResult> GetAwaiter() => _task.GetAwaiter();
    }
}