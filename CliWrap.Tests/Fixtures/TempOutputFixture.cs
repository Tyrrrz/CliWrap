using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CliWrap.Tests.Fixtures;

public class TempOutputFixture : IDisposable
{
    private readonly string _rootDirPath = Path.Combine(
        Path.GetDirectoryName(typeof(TempOutputFixture).Assembly.Location) ?? Directory.GetCurrentDirectory(),
        "Temp"
    );

    private readonly ConcurrentBag<string> _dirPaths = new();
    private readonly ConcurrentBag<string> _filePaths = new();

    public string GetTempDirPath(string dirName)
    {
        var dirPath = Path.Combine(_rootDirPath, dirName);

        Directory.CreateDirectory(dirPath);
        _dirPaths.Add(dirPath);

        return dirPath;
    }

    public string GetTempDirPath() => GetTempDirPath($"Test-{Guid.NewGuid()}");

    public string GetTempFilePath(string fileName)
    {
        var filePath = Path.Combine(_rootDirPath, fileName);

        var dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
            Directory.CreateDirectory(dirPath);

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

        foreach (var dirPath in _dirPaths)
        {
            try
            {
                Directory.Delete(dirPath, true);
            }
            catch (DirectoryNotFoundException)
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