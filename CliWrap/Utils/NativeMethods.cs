using System.Runtime.InteropServices;

namespace CliWrap.Utils;

internal static partial class NativeMethods
{
    public static partial class Unix
    {
        // LibraryImport requires the source generator, which is only available on .NET 7+.
        // Polyfills cannot replicate this because the generator itself is not available on older targets.
#if NET7_0_OR_GREATER
        [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static partial int Kill(int pid, int sig);
#else
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int pid, int sig);
#endif
    }
}
