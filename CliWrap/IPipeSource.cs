using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap;

/// <summary>
/// Abstraction that represents an inwards-facing pipe.
/// </summary>
public interface IPipeSource
{
    /// <summary>
    /// Copies the binary content pushed to the pipe into the destination stream.
    /// </summary>
    public Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default);
}