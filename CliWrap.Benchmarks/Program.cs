using BenchmarkDotNet.Running;

namespace CliWrap.Benchmarks;

public static class Program
{
    public static void Main() => BenchmarkRunner.Run(typeof(Program).Assembly);
}