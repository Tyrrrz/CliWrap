using System;
using System.Collections.Generic;
using System.Globalization;

namespace CliWrap
{
    public class CliArgumentFormatter
    {
        private readonly List<string> _args = new List<string>();

        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        public CliArgumentFormatter SetCultureInfo(CultureInfo cultureInfo)
        {
            _cultureInfo = cultureInfo;
            return this;
        }

        public CliArgumentFormatter AddArgument(string value)
        {
            _args.Add(value);
            return this;
        }

        public CliArgumentFormatter AddArguments(IEnumerable<string> values)
        {
            foreach (var value in values)
                AddArgument(value);

            return this;
        }

        public CliArgumentFormatter AddArgument(IFormattable value) =>
            AddArgument(value.ToString(null, _cultureInfo));

        public CliArgumentFormatter AddArguments(IEnumerable<IFormattable> values)
        {
            foreach (var value in values)
                AddArgument(value);

            return this;
        }

        public override string ToString() => string.Join(" ", _args);
    }
}