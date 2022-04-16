using System.Reactive.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.EventStream;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PushEventStreamBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = $"{Tests.Dummy.Program.FilePath} generate text --lines 1000";

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<int> ExecuteWithCliWrap_Observable()
    {
        var counter = 0;

        await Cli.Wrap(FilePath).WithArguments(Args).Observe().ForEachAsync(cmdEvent =>
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
        });

        return counter;
    }
}