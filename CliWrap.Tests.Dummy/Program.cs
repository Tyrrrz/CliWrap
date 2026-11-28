using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CliFx;

[assembly: ExcludeFromCodeCoverage]

namespace CliWrap.Tests.Dummy;

public static class Program
{
    // Path to the apphost
    public static string FilePath { get; } =
        Path.ChangeExtension(
            Assembly.GetExecutingAssembly().Location,
            OperatingSystem.IsWindows() ? "exe" : null
        );

    public static async Task<int> Main(string[] args)
    {
        // Make sure color codes are not produced because we rely on the output in tests
        Environment.SetEnvironmentVariable("FORCE_COLOR", null);
        Environment.SetEnvironmentVariable("NO_COLOR", "1");
        Environment.SetEnvironmentVariable(
            "DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION",
            null
        );

        return await new CommandLineApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync(args);
    }
}
