using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliWrap.Buffered;
using Cysharp.Diagnostics;
using RunProcessAsTask;
using Sheller.Implementations.Shells;

namespace CliWrap.Benchmarks;

[MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BufferingBenchmarks
{
    private const string FilePath = "dotnet";
    private static readonly string Args = $"{Tests.Dummy.Program.FilePath} generate text --lines 1000";

    [Benchmark(Description = "CliWrap", Baseline = true)]
    public async Task<(string, string)> ExecuteWithCliWrap()
    {
        var result = await Cli.Wrap(FilePath).WithArguments(Args).ExecuteBufferedAsync();
        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark(Description = "RunProcessAsTask")]
    public async Task<(string, string)> ExecuteWithRunProcessAsTask()
    {
        var result = await ProcessEx.RunAsync(FilePath, Args);
        return (string.Join(Environment.NewLine, result.StandardOutput), string.Join(Environment.NewLine, result.StandardError));
    }

    [Benchmark(Description = "Sheller")]
    public async Task<(string, string)> ExecuteWithSheller()
    {
        var result = await Sheller.Builder.UseShell<Cmd>().ExecuteCommandAsync(FilePath, new[] { Args });
        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark(Description = "MedallionShell")]
    public async Task<(string, string)> ExecuteWithMedallionShell()
    {
        var result = await Medallion.Shell.Shell.Default.Run(FilePath, Args.Split(' ')).Task;
        return (result.StandardOutput, result.StandardError);
    }

    [Benchmark(Description = "ProcessX")]
    public async Task<(string, string)> ExecuteWithProcessX()
    {
        var (_, stdOutStream, stdErrStream) = ProcessX.GetDualAsyncEnumerable(FilePath, arguments: Args);
        var stdOutTask = stdOutStream.ToTask();
        var stdErrTask = stdErrStream.ToTask();

        await Task.WhenAll(stdOutTask, stdErrTask);

        return (string.Join(Environment.NewLine, stdOutTask.Result), string.Join(Environment.NewLine, stdErrTask.Result));
    }
}