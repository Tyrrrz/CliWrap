using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.EventStream;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PullEventStreamBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<int> CliWrap()
    {
        var counter = 0;

        await foreach (
            var cmdEvent in Cli.Wrap(Tests.Dummy.Program.FilePath)
                .WithArguments(["generate", "text", "--length", "100000000", "--lines", "1000"])
                .ListenAsync()
        )
        {
            switch (cmdEvent)
            {
                case StandardOutputCommandEvent:
                    counter++;
                    break;
                case StandardErrorCommandEvent:
                    counter++;
                    break;
            }
        }

        return counter;
    }

    [Benchmark]
    public async Task<int> ProcessX()
    {
        var counter = 0;

        var (_, stdOutStream, stdErrStream) = Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable(
            Tests.Dummy.Program.FilePath,
            arguments: "generate text --length 100000000 --lines 1000"
        );

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
