using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace CliWrap.Benchmarks
{
    [MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class HalfDuplexStreamBenchmarks
    {
        private const string FilePath = "dotnet";
        private static readonly string Args = $"{Tests.Dummy.Program.FilePath} {Tests.Dummy.Program.PrintRandomBinary} 1000000 100000";

        [Benchmark(Description = "CliWrap", Baseline = true)]
        public async Task<int> ExecuteWithCliWrap()
        {
            await using var stream1 = new MemoryStream();
            await using var stream2 = new MemoryStream();
            
            var result =  await (Cli.Wrap(FilePath).WithArguments(Args) | PipeTarget.Merge(PipeTarget.ToStream(stream1), PipeTarget.ToStream(stream2))).ExecuteAsync();
            return result.ExitCode;
        }
    }
}