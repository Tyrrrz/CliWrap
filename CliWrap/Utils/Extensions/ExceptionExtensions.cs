using System;
using System.Linq;

namespace CliWrap.Utils.Extensions;

internal static class ExceptionExtensions
{
    public static Exception? TryGetSingle(this AggregateException exception)
    {
        var exceptions = exception.Flatten().InnerExceptions;

        return exceptions.Count == 1 ? exceptions.Single() : null;
    }
}
