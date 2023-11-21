using System.IO;
using Contextual;

namespace CliWrap.Magic.Contexts;

internal class WorkingDirectoryContext : Context
{
    public string Path { get; }

    public WorkingDirectoryContext(string path) => Path = path;

    public WorkingDirectoryContext()
        : this(Directory.GetCurrentDirectory()) { }
}
