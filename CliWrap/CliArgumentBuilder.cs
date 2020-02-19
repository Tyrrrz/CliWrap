﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CliWrap
{
    public class CliArgumentBuilder
    {
        private static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

        private readonly List<string> _args = new List<string>();

        public CliArgumentBuilder Add(string value)
        {
            _args.Add(value);
            return this;
        }

        public CliArgumentBuilder Add(IEnumerable<string> values)
        {
            foreach (var value in values)
                Add(value);

            return this;
        }

        public CliArgumentBuilder Add(IFormattable value, CultureInfo cultureInfo) =>
            Add(value.ToString(null, cultureInfo));

        public CliArgumentBuilder Add(IFormattable value) =>
            Add(value, DefaultCulture);

        public CliArgumentBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo)
        {
            foreach (var value in values)
                Add(value, cultureInfo);

            return this;
        }

        public CliArgumentBuilder Add(IEnumerable<IFormattable> values) =>
            Add(values, DefaultCulture);

        public string Build()
        {
            var buffer = new StringBuilder();

            foreach (var arg in _args)
            {
                // If buffer has something in it - append a space
                if (buffer.Length != 0)
                    buffer.Append(' ');

                // If argument is clean and doesn't need escaping - append it directly
                if (arg.Length != 0 && arg.All(c => !char.IsWhiteSpace(c) && c != '"'))
                {
                    buffer.Append(arg);
                }
                // Otherwise - escape problematic characters
                else
                {
                    // Escaping logic taken from CoreFx source code

                    buffer.Append('"');

                    for (var i = 0; i < arg.Length;)
                    {
                        var c = arg[i++];

                        if (c == '\\')
                        {
                            var numBackSlash = 1;
                            while (i < arg.Length && arg[i] == '\\')
                            {
                                numBackSlash++;
                                i++;
                            }

                            if (i == arg.Length)
                            {
                                buffer.Append('\\', numBackSlash * 2);
                            }
                            else if (arg[i] == '"')
                            {
                                buffer.Append('\\', numBackSlash * 2 + 1);
                                buffer.Append('"');
                                i++;
                            }
                            else
                            {
                                buffer.Append('\\', numBackSlash);
                            }
                        }
                        else if (c == '"')
                        {
                            buffer.Append('\\');
                            buffer.Append('"');
                        }
                        else
                        {
                            buffer.Append(c);
                        }
                    }

                    buffer.Append('"');
                }
            }

            return buffer.ToString();
        }
    }
}