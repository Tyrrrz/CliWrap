using System;
using System.IO;

namespace CliWrap.Utils;

/// <summary>
/// Abstract base class for platform-specific pseudo-terminal implementations.
/// </summary>
internal abstract class PseudoTerminal : IDisposable
{
    /// <summary>
    /// Gets whether PTY is supported on the current platform.
    /// </summary>
    public static bool IsSupported
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                // ConPTY requires Windows 10 version 1809 (build 17763) or later
                return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Creates a platform-specific pseudo-terminal.
    /// </summary>
    /// <param name="columns">Terminal width in columns.</param>
    /// <param name="rows">Terminal height in rows.</param>
    /// <returns>A new pseudo-terminal instance.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when PTY is not supported on the current platform.
    /// </exception>
    public static PseudoTerminal Create(int columns, int rows)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                throw new PlatformNotSupportedException(
                    "Pseudo-terminal support requires Windows 10 version 1809 (build 17763) or later. "
                        + $"Current version: {Environment.OSVersion.Version}."
                );
            }

            return new WindowsPseudoTerminal(columns, rows);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return new UnixPseudoTerminal(columns, rows);
        }

        throw new PlatformNotSupportedException(
            $"Pseudo-terminal support is not available on {Environment.OSVersion.Platform}."
        );
    }

    /// <summary>
    /// Gets the stream for writing to the PTY (stdin of the child process).
    /// </summary>
    public abstract Stream InputStream { get; }

    /// <summary>
    /// Gets the stream for reading from the PTY (stdout of the child process).
    /// </summary>
    public abstract Stream OutputStream { get; }

    /// <summary>
    /// Resizes the terminal to the specified dimensions.
    /// </summary>
    /// <param name="columns">New terminal width in columns.</param>
    /// <param name="rows">New terminal height in rows.</param>
    public abstract void SetSize(int columns, int rows);

    /// <summary>
    /// Closes the console connection to signal EOF on the output stream.
    /// This causes blocked reads on the output stream to return.
    /// </summary>
    /// <remarks>
    /// This should be called after the process exits to allow output piping to complete.
    /// The streams remain valid until Dispose is called.
    /// </remarks>
    public abstract void CloseConsole();

    /// <summary>
    /// Releases all resources used by the pseudo-terminal.
    /// </summary>
    public abstract void Dispose();
}
