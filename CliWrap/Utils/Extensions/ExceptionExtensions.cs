using System;
using System.Linq;

namespace CliWrap.Utils.Extensions;

internal static class ExceptionExtensions
{
    extension(AggregateException exception)
    {
        public Exception? TryGetSingle()
        {
            var exceptions = exception.Flatten().InnerExceptions;

            return exceptions.Count == 1 ? exceptions.Single() : null;
        }
    }
}
