using System.Threading.Tasks;
using CliFx;

namespace CliWrap.Tests.Dummy;

public static class Program
{
    public static string FilePath { get; } = typeof(Program).Assembly.Location;

    public static async Task<int> Main(string[] args) =>
        await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync(args);
}