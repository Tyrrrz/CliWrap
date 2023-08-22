using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace CliWrap.Tests.Dummy;

public static class Program
{
    // Path to the apphost
    public static string FilePath { get; } =
        Path.ChangeExtension(
            Assembly.GetExecutingAssembly().Location,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "exe" : null
        );

    public static async Task<int> Main(string[] args)
    {
        // Make sure color codes are not produced because we rely on the output in tests
        Environment.SetEnvironmentVariable(
            "DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION",
            "false"
        );

        return await new CliApplicationBuilder()
            .AddCommandsFromThisAssembly()
            .Build()
            .RunAsync(args);
    }
}
