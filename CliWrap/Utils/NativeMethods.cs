using System;
using System.Runtime.InteropServices;

namespace CliWrap.Utils;

internal static class NativeMethods
{
    public static class Windows
    {
        public delegate bool ConsoleCtrlDelegate(uint dwCtrlEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handlerRoutine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();
    }

    public static class Unix
    {
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int pid, int sig);
    }
}