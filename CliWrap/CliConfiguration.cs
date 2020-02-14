using System.Diagnostics;
using System.IO;

namespace CliWrap
{
    public partial class CliConfiguration
    {
        public string WorkingDirPath { get; }

        public string Arguments { get; }

        public CliConfiguration(string workingDirPath, string arguments)
        {
            WorkingDirPath = workingDirPath;
            Arguments = arguments;
        }

        internal ProcessStartInfo GetStartInfo(string filePath) => new ProcessStartInfo
        {
            FileName = filePath,
            WorkingDirectory = WorkingDirPath,
            Arguments = Arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    public partial class CliConfiguration
    {
        public static CliConfiguration Default { get; } = new CliConfiguration(
            Directory.GetCurrentDirectory(),
            "");
    }
}