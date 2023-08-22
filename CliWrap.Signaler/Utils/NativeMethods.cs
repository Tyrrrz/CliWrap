using System.Runtime.InteropServices;

namespace CliWrap.Signaler.Utils;

internal static class NativeMethods
{
    public static class Windows
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        public delegate bool ConsoleCtrlDelegate(uint dwCtrlEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(
            ConsoleCtrlDelegate? handlerRoutine,
            bool add
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    }
}
