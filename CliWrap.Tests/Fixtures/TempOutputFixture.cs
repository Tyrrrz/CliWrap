using System;
using System.IO;

namespace CliWrap.Tests.Fixtures
{
    public class TempOutputFixture : IDisposable
    {
        public string DirPath { get; } = Path.Combine(
            Path.GetDirectoryName(typeof(TempOutputFixture).Assembly.Location) ?? Directory.GetCurrentDirectory(),
            "Temp"
        );

        public TempOutputFixture() => Directory.CreateDirectory(DirPath);

        public string GetTempFilePath(string fileName) => Path.Combine(DirPath, fileName);

        public string GetTempFilePath() => GetTempFilePath(Guid.NewGuid().ToString());

        public void Dispose()
        {
            try
            {
                Directory.Delete(DirPath, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }
}