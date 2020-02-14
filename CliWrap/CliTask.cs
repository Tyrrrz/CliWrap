using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CliWrap
{
    public class CliTask
    {
        private readonly Task<CliResult> _task;

        public int ProcessId { get; }

        public CliTask(Task<CliResult> task, int processId)
        {
            _task = task;
            ProcessId = processId;
        }

        public TaskAwaiter<CliResult> GetAwaiter() => _task.GetAwaiter();
    }
}