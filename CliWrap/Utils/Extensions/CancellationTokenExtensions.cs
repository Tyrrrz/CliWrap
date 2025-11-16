using System;
using System.Threading;

namespace CliWrap.Utils.Extensions;

internal static class CancellationTokenExtensions
{
    extension(CancellationToken cancellationToken)
    {
        public void ThrowIfCancellationRequested(string message)
        {
            if (!cancellationToken.IsCancellationRequested)
                return;

            throw new OperationCanceledException(message, cancellationToken);
        }
    }
}
