using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.EventStream;
using Cysharp.Diagnostics;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PullEventStreamBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = $"{Tests.Dummy.Program.FilePath} generate text --lines 1000";

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<int> ExecuteWithCliWrap_Async()
    {
        var counter = 0;

        await foreach (var cmdEvent in Cli.Wrap(FilePath).WithArguments(Args).ListenAsync())
        {
            switch (cmdEvent)
            {
                case StandardOutputCommandEvent _:
                    counter++;
                    break;
                case StandardErrorCommandEvent _:
                    counter++;
                    break;
            }
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