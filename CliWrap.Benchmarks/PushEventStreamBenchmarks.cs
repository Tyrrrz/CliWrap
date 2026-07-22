using System.Reactive.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.EventStream;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PushEventStreamBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<int> CliWrap()
    {
        var counter = 0;

        await Cli.Wrap(Tests.Dummy.Program.FilePath)
            .WithArguments(["generate", "text", "--length", "100000000", "--lines", "1000"])
            .Observe()
            .ForEachAsync(cmdEvent =>
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
            });

        return counter;
    }
}
