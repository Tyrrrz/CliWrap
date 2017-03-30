using System;
using System.Collections.Generic;
using System.Text;

namespace CliWrap.Formatters
{
    /// <summary>
    /// Formats arguments by prepending names with a dash and values with a space
    /// </summary>
    public class DashSpacedArgumentFormatter : IArgumentFormatter
    {
        /// <inheritdoc />
        public string Format(IEnumerable<Argument> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var buffer = new StringBuilder();

            foreach (var arg in arguments)
            {
                // Bool
                if (arg.Value is bool)
                {
                    bool value = (bool) arg.Value;

                    // True
                    if (value)
                    {
                        buffer.Append('-');
                        buffer.Append(arg.Name);
                        buffer.Append(' ');
                    }
                    // False values are omitted
                }
                // Not bool
                else
                {
                    string value = arg.Value?.ToString();

                    // Not null
                    if (value != null)
                    {
                        buffer.Append('-');
                        buffer.Append(arg.Name);
                        buffer.Append(' ');
                        buffer.Append(value);
                        buffer.Append(' ');
                    }
                    // Null values are omitted
                }
            }

            return buffer.ToString().Trim();
        }
    }
}