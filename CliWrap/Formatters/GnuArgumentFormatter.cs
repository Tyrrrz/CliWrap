using System;
using System.Collections.Generic;
using System.Text;

namespace CliWrap.Formatters
{
    /// <summary>
    /// Formats arguments according to the GNU command line guidelines.
    /// </summary>
    public class GnuArgumentFormatter : IArgumentFormatter
    {
        private string FormatArgumentPrefix(string argumentName)
        {
            if (argumentName == null)
                throw new ArgumentNullException(nameof(argumentName));

            // Short name
            if (argumentName.Length == 1)
                return "-";

            // Full name
            return "--";
        }

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
                        buffer.Append(FormatArgumentPrefix(arg.Name));
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
                        buffer.Append(FormatArgumentPrefix(arg.Name));
                        buffer.Append(arg.Name);
                        buffer.Append('=');
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