using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.EventStream;
using Cysharp.Diagnostics;

namespace CliWrap.Benchmarks
{
    [MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class EventStreamingBenchmarks
    {
        private const string FilePath = "dotnet";
        private static readonly string Args = $"{Tests.Dummy.Program.FilePath} {Tests.Dummy.Program.PrintLines} 100000";

        [Benchmark(Description = "CliWrap", Baseline = true)]
        public async Task<int> ExecuteWithCliWrap()
        {
            var counter = 0;
            await foreach (var cmdEvent in Cli.Wrap(FilePath).WithArguments(Args).ListenAsync())
            {
                cmdEvent
                    .OnStandardOutput(_ => counter++)
                    .OnStandardError(_ => counter++);
            }

            return counter;
        }

        [Benchmark(Description = "ProcessX")]
        public async Task<int> ExecuteWithProcessX()
        {
            var counter = 0;

            var (_, stdOutStream, stdErrStream) = ProcessX.GetDualAsyncEnumerable(FilePath, arguments: Args);

            var consumeStdOutTask = Task.Run(async () =>
            {
                await foreach (var _ in stdOutStream)
                {
                    counter++;
                }
            });

            var consumeStdErrorTask = Task.Run(async () =>
            {
                await foreach (var _ in stdErrStream)
                {
                    counter++;
                }
            });

            await Task.WhenAll(consumeStdOutTask, consumeStdErrorTask);

            return counter;
        }
    }
}