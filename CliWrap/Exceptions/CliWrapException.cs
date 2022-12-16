using System;
using System.Diagnostics.CodeAnalysis;

namespace CliWrap.Exceptions;

/// <summary>
/// Parent class for exceptions thrown by <see cref="CliWrap" />.
/// </summary>
public abstract class CliWrapException : Exception
{
    /// <summary>
    /// Initializes an instance of <see cref="CliWrapException" />.
    /// </summary>
    protected CliWrapException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="CliWrapException" />.
    /// </summary>
    // TODO: (breaking change) remove in favor of an optional parameter in the constructor above
    [ExcludeFromCodeCoverage]
    protected CliWrapException(string message)
        : this(message, null)
    {
    }
}