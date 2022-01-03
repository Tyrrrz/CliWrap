using System;

namespace CliWrap.Utils;

internal static class PathResolver
{ 
    internal static string ResolvePath(string fileName)
    {
        // Update target path if necessary
        // We should use 'Path.IsPathFullyQualified', but its not available in .NET Standard 2.0
        // https://github.com/dotnet/runtime/issues/22796
        if (!PathResolver || Path.IsPathRooted(TargetFilePath)) return fileName;
        
        if (File.Exists(fileName))
            return Path.GetFullPath(fileName);

        var envValues = Environment.GetEnvironmentVariable("PATH");
        if (envValues == null) return fileName;
        foreach (var path in envValues.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, fileName);
            
            // we should look for .exe and .cmd files on windows
            if (!fileName.EndsWith(".exe") && !fileName.EndsWith(".cmd") &&
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (File.Exists(fullPath + ".exe"))
                    return fullPath + ".exe";
                if (File.Exists(fullPath + ".cmd"))
                    return fullPath + ".cmd";
            }
            else if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        // If we can't find the file, return the original path
        return fileName;
    }
}

 