using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using RunProcessAsTask;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BasicBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = Tests.Dummy.Program.FilePath;

    [Benchmark(Baseline = true)]
    public async Task<int> CliWrap()
    {
        var result = await Cli.Wrap(FilePath).WithArguments(Args).ExecuteAsync();
        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> RunProcessAsTask()
    {
        var result = await ProcessEx.RunAsync(FilePath, Args);
        return result.ExitCode;
    }

    [Benchmark]
    public async Task<int> MedallionShell()
    {
        var result = await Medallion.Shell.Command.Run(FilePath, Args).Task;
        return result.ExitCode;
    }
}
