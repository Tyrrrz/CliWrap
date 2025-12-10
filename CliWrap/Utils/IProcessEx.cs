using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

/// <summary>
/// Common interface for process wrappers (standard and PTY-based).
/// </summary>
internal interface IProcessEx : IDisposable
{
    int Id { get; }

    string Name { get; }

    Stream StandardInput { get; }

    Stream StandardOutput { get; }

    Stream StandardError { get; }

    DateTimeOffset StartTime { get; }

    DateTimeOffset ExitTime { get; }

    int ExitCode { get; }

    void Interrupt();

    void Kill();

    Task WaitUntilExitAsync(CancellationToken cancellationToken = default);
}
