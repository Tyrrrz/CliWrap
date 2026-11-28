using System;
using System.Threading;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace CliWrap.Utils;

// Kept in a separate class so that the handlers are only registered when the token is actually used
internal static class ShutdownSignal
{
    private static readonly CancellationTokenSource Cts = new();

#if NET6_0_OR_GREATER
    // Must be kept alive, otherwise the registration is removed when it gets garbage-collected
    private static readonly PosixSignalRegistration? SigTermRegistration;
#endif

    public static CancellationToken Token => Cts.Token;

    static ShutdownSignal()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Trigger();

#if NET6_0_OR_GREATER
        // The runtime doesn't raise ProcessExit when the process is terminated by an unhandled SIGTERM,
        // so we need to listen to it separately. The signal context is left untouched, so the default
        // handling (or the handling configured by the user) still applies afterwards.
        try
        {
            SigTermRegistration = PosixSignalRegistration.Create(
                PosixSignal.SIGTERM,
                _ => Trigger()
            );
        }
        catch (PlatformNotSupportedException)
        {
            // Not every platform supports signal handling
        }
#endif
    }

    private static void Trigger()
    {
        try
        {
            // Callbacks run synchronously, so the processes are killed before the handler returns
            Cts.Cancel();
        }
        catch
        {
            // The process is exiting anyway, so there's nothing useful to do with the exception
        }
    }
}
