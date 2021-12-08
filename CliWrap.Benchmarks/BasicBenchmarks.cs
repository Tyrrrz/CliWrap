using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using RunProcessAsTask;
using Sheller.Implementations.Shells;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BasicBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = Tests.Dummy.Program.FilePath;

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<int> ExecuteWithCliWrap()
    {
        var result = await Cli.Wrap(FilePath).WithArguments(Args).ExecuteAsync();
        return result.ExitCode;
    }

    [Benchmark(Description = "RunProcessAsTask")]
    public async Task<int> ExecuteWithRunProcessAsTask()
    {
        var result = await ProcessEx.RunAsync(FilePath, Args);
        return result.ExitCode;
    }

    [Benchmark(Description = "Sheller")]
    public async Task<int> ExecuteWithSheller()
    {
        var result = await Sheller.Builder.UseShell<Cmd>().ExecuteCommandAsync(FilePath, new[] {Args});
        return result.ExitCode;
    }

    [Benchmark(Description = "MedallionShell")]
    public async Task<int> ExecuteWithMedallionShell()
    {
        var result = await Medallion.Shell.Command.Run(FilePath, Args).Task;
        return result.ExitCode;
    }
}