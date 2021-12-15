using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap;

/// <summary>
/// Abstraction that represents an outwards-facing pipe.
/// </summary>
public interface IPipeTarget
{
    /// <summary>
    /// Copies the binary content from the stream and pushes it into the pipe.
    /// </summary>
    Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default);
}