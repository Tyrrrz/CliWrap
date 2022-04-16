using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipeToStreamBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = $"{Tests.Dummy.Program.FilePath} generate binary";

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<Stream> ExecuteWithCliWrap_PipeToStream()
    {
        await using var stream = new MemoryStream();

        var command = Cli.Wrap(FilePath).WithArguments(Args) | stream;
        await command.ExecuteAsync();

        return stream;
    }

    [Benchmark(Description = "MedallionShell")]
    public async Task<Stream> ExecuteWithMedallionShell_PipeToStream()
    {
        await using var stream = new MemoryStream();

        var command = Medallion.Shell.Command.Run(FilePath, Args.Split(' ')) > stream;
        await command.Task;

        return stream;
    }
}