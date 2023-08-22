using System;
using System.Threading;

namespace CliWrap.Utils.Extensions;

internal static class CancellationTokenExtensions
{
    public static void ThrowIfCancellationRequested(
        this CancellationToken cancellationToken,
        string message
    )
    {
        if (!cancellationToken.IsCancellationRequested)
            return;

        throw new OperationCanceledException(message, cancellationToken);
    }
}
