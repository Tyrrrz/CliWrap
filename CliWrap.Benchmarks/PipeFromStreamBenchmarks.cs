using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PipeFromStreamBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = $"{Tests.Dummy.Program.FilePath} echo stdin";

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<Stream> ExecuteWithCliWrap_PipeToStream()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var command = stream | Cli.Wrap(FilePath).WithArguments(Args);
        await command.ExecuteAsync();

        return stream;
    }

    [Benchmark(Description = "MedallionShell")]
    public async Task<Stream> ExecuteWithMedallionShell_PipeToStream()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var command = Medallion.Shell.Command.Run(FilePath, Args.Split(' ')) < stream;
        await command.Task;

        return stream;
    }
}