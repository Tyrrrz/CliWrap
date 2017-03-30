using System;
using System.Collections.Generic;
using System.Text;

namespace CliWrap.Formatters
{
    /// <summary>
    /// Formats arguments by separating names and values with spaces
    /// </summary>
    public class SpacedArgumentFormatter : IArgumentFormatter
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
                    bool value = (bool)arg.Value;

                    // True
                    if (value)
                    {
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