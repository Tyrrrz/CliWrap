using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.Buffered;
using RunProcessAsTask;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BufferingBenchmarks
{
    [Benchmark(Baseline = true)]
    public async Task<(string, string)> CliWrap()
    {
        var result = await Cli.Wrap(Tests.Dummy.Program.FilePath)
            .WithArguments(["generate", "text", "--length", "100000000", "--lines", "1000"])
            .ExecuteBufferedAsync();

        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark]
    public async Task<(string, string)> RunProcessAsTask()
    {
        var result = await ProcessEx.RunAsync(
            Tests.Dummy.Program.FilePath,
            "generate text --length 100000000 --lines 1000"
        );

        return (
            string.Join(Environment.NewLine, result.StandardOutput),
            string.Join(Environment.NewLine, result.StandardError)
        );
    }

    [Benchmark]
    public async Task<(string, string)> MedallionShell()
    {
        var result = await Medallion
            .Shell.Shell.Default.Run(
                Tests.Dummy.Program.FilePath,
                ["generate", "text", "--length", "100000000", "--lines", "1000"]
            )
            .Task;

        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark]
    public async Task<(string, string)> ProcessX()
    {
        var (_, stdOutStream, stdErrStream) = Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable(
            Tests.Dummy.Program.FilePath,
            arguments: "generate text --length 100000000 --lines 1000"
        );

        var stdOutTask = stdOutStream.ToTask();
        var stdErrTask = stdErrStream.ToTask();

        await Task.WhenAll(stdOutTask, stdErrTask);

        return (
            string.Join(Environment.NewLine, stdOutTask.Result),
            string.Join(Environment.NewLine, stdErrTask.Result)
        );
    }
}
