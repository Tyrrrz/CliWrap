using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using CliWrap.Utils.Extensions;

namespace CliWrap.Utils;

internal partial class WindowsSignaler(string filePath) : IDisposable
{
    public bool TrySend(int processId, int signalId)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments =
                    processId.ToString(CultureInfo.InvariantCulture)
                    + ' '
                    + signalId.ToString(CultureInfo.InvariantCulture),
                CreateNoWindow = true,
                UseShellExecute = false,
                Environment =
                {
                    // This is a .NET 3.5 executable, so we need to configure framework rollover
                    // to allow it to also run against .NET 4.0 and higher.
                    // https://gist.github.com/MichalStrehovsky/d6bc5e4d459c23d0cf3bd17af9a1bcf5
                    ["COMPLUS_OnlyUseLatestCLR"] = "1",
                },
            },
        };

        if (!process.Start())
            return false;

        if (!process.WaitForExit(30_000))
            return false;

        return process.ExitCode == 0;
    }

    public void Dispose()
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            Debug.Fail("Failed to delete the signaler executable.");
        }
    }
}

internal partial class WindowsSignaler
{
    public static WindowsSignaler Deploy()
    {
        // Signaler executable is embedded inside this library as a resource
        var filePath = Path.Combine(Path.GetTempPath(), $"CliWrap.Signaler.{Guid.NewGuid()}.exe");
        Assembly.GetExecutingAssembly().ExtractManifestResource("CliWrap.Signaler.exe", filePath);

        return new WindowsSignaler(filePath);
    }
}
