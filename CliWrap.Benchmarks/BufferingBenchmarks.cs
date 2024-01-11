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
    private const string FilePath = "dotnet";
    private static readonly string Args =
        $"{Tests.Dummy.Program.FilePath} generate text --lines 1000";

    [Benchmark(Baseline = true)]
    public async Task<(string, string)> CliWrap()
    {
        var result = await Cli.Wrap(FilePath).WithArguments(Args).ExecuteBufferedAsync();
        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark]
    public async Task<(string, string)> RunProcessAsTask()
    {
        var result = await ProcessEx.RunAsync(FilePath, Args);

        return (
            string.Join(Environment.NewLine, result.StandardOutput),
            string.Join(Environment.NewLine, result.StandardError)
        );
    }

    [Benchmark]
    public async Task<(string, string)> MedallionShell()
    {
        var result = await Medallion.Shell.Shell.Default.Run(FilePath, Args.Split(' ')).Task;
        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark]
    public async Task<(string, string)> ProcessX()
    {
        var (_, stdOutStream, stdErrStream) = Cysharp.Diagnostics.ProcessX.GetDualAsyncEnumerable(
            FilePath,
            arguments: Args
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
