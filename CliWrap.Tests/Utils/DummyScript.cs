using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CliWrap.Tests.Utils;

// Runs the dummy program either through the EXE file or via .NET CLI, depending on the platform
internal static class DummyScript
{
    public static string DirPath { get; } =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        Directory.GetCurrentDirectory();

    public static string FilePath { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Path.Combine(DirPath, "run-dummy.bat")
        : Path.Combine(DirPath, "run-dummy.sh");

    static DummyScript()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            File.SetUnixFileMode(FilePath, UnixFileMode.UserExecute);
    }
}