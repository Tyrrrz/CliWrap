using System.Collections.Generic;

namespace CliWrap.Formatters
{
    /// <summary>
    /// Formats command line arguments
    /// </summary>
    public interface IArgumentFormatter
    {
        /// <summary>
        /// Formats arguments to a composite argument string
        /// </summary>
        string Format(IEnumerable<Argument> arguments);
    }
}