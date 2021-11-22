using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CliWrap.Tests.Fixtures
{
    public class TempOutputFixture : IDisposable
    {
        private readonly string _dirPath =
            Path.GetDirectoryName(typeof(TempOutputFixture).Assembly.Location) ??
            Directory.GetCurrentDirectory();

        private readonly ConcurrentBag<string> _filePaths = new();

        public string GetTempFilePath(string fileName)
        {
            var filePath = Path.Combine(_dirPath, fileName);
            _filePaths.Add(filePath);

            return filePath;
        }

        public string GetTempFilePath() => GetTempFilePath($"Test-{Guid.NewGuid()}.tmp");

        public void Dispose()
        {
            var exceptions = new List<Exception>();

            foreach (var filePath in _filePaths)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (FileNotFoundException)
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }
    }
}