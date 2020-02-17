using System;
using System.Collections.Generic;
using System.Globalization;

namespace CliWrap
{
    public class CliArgumentBuilder
    {
        private static readonly CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

        private readonly List<string> _args = new List<string>();

        public CliArgumentBuilder AddArgument(string value)
        {
            _args.Add(value);
            return this;
        }

        public CliArgumentBuilder AddArguments(IEnumerable<string> values)
        {
            foreach (var value in values)
                AddArgument(value);

            return this;
        }

        public CliArgumentBuilder AddArgument(IFormattable value, CultureInfo cultureInfo) =>
            AddArgument(value.ToString(null, cultureInfo));

        public CliArgumentBuilder AddArgument(IFormattable value) =>
            AddArgument(value, DefaultCulture);

        public CliArgumentBuilder AddArguments(IEnumerable<IFormattable> values, CultureInfo cultureInfo)
        {
            foreach (var value in values)
                AddArgument(value, cultureInfo);

            return this;
        }

        public CliArgumentBuilder AddArguments(IEnumerable<IFormattable> values) =>
            AddArguments(values, DefaultCulture);

        public string Build() => string.Join(" ", _args);

        public override string ToString() => Build();
    }
}