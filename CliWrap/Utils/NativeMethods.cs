using System.Runtime.InteropServices;

namespace CliWrap.Utils;

internal static partial class NativeMethods
{
    public static partial class Unix
    {
        // LibraryImport's source generator doesn't emit an implementation for pre-.NET 7 targets
        // because the generated code relies on runtime APIs introduced in .NET 7.
#if NET7_0_OR_GREATER
        [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static partial int Kill(int pid, int sig);
#else
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int pid, int sig);
#endif
    }
}
