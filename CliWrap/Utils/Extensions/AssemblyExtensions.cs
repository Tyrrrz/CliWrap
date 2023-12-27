using System.IO;
using System.Reflection;
using System.Resources;

namespace CliWrap.Utils.Extensions;

internal static class AssemblyExtensions
{
    public static void ExtractManifestResource(
        this Assembly assembly,
        string resourceName,
        string destFilePath
    )
    {
        var input =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new MissingManifestResourceException(
                $"Failed to find resource '{resourceName}'."
            );

        using var output = File.Create(destFilePath);
        input.CopyTo(output);
    }
}
