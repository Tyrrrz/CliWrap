using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cysharp.Diagnostics;
using RunProcessAsTask;
using Sheller.Implementations.Shells;

namespace CliWrap.Benchmarks
{
    [MemoryDiagnoser]
    public class ExecuteGetStdOutBenchmarks
    {
        private const string FilePath = "dotnet";
        private static readonly string Args = $"{Tests.Dummy.Program.Location} {Tests.Dummy.Program.LoopStdOut} 100000";

        [Benchmark(Description = "CliWrap", Baseline = true)]
        public async Task<string> ExecuteWithCliWrap()
        {
            var result = await Cli.Wrap(FilePath, Args).Buffered().ExecuteAsync();

            return result.StandardOutput;
        }

        [Benchmark(Description = "RunProcessAsTask")]
        public async Task<string> ExecuteWithRunProcessAsTask()
        {
            var result = await ProcessEx.RunAsync(FilePath, Args);
            return string.Join(Environment.NewLine, result.StandardOutput);
        }

        [Benchmark(Description = "Sheller")]
        public async Task<string> ExecuteWithSheller()
        {
            var result = await Sheller.Builder.UseShell<Cmd>().ExecuteCommandAsync(FilePath, new[] {Args});
            return result.StandardOutput;
        }

        [Benchmark(Description = "MedallionShell")]
        public async Task<string> ExecuteWithMedallionShell()
        {
            var result = await Medallion.Shell.Shell.Default.Run(FilePath, Args).Task;
            return result.StandardOutput;
        }

        [Benchmark(Description = "ProcessX")]
        public async Task<string> ExecuteWithProcessX()
        {
            var result = await ProcessX.StartAsync(FilePath, arguments: Args).ToTask();
            return string.Join(Environment.NewLine, result);
        }
    }
}