using System;
using System.Runtime.InteropServices;

namespace CliWrap.Native;

// Platform-specific native methods for pseudo-terminal support.
// These are kept separate from NativeMethods.cs to avoid polluting it with PTY-specific code.
internal static partial class PtyNativeMethods
{
    public static partial class Unix
    {
        // ── openpty / forkpty ──────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct WinSize
        {
            public ushort Row;
            public ushort Col;
            public ushort XPixel;
            public ushort YPixel;
        }

        // macOS ships openpty in libutil; Linux ships it in libc (glibc >= 2.1).
        // We emit two separate DllImport attributes (one per library name) inside
        // platform-guarded methods rather than trying to pick the right name at call time.

        [DllImport("libutil", EntryPoint = "forkpty", SetLastError = true)]
        private static extern int ForkPtyMacOS(
            out int amaster,
            IntPtr name,
            IntPtr termios,
            ref WinSize winsize
        );

        [DllImport("libc", EntryPoint = "forkpty", SetLastError = true)]
        private static extern int ForkPtyLinux(
            out int amaster,
            IntPtr name,
            IntPtr termios,
            ref WinSize winsize
        );

        /// <summary>
        /// Forks the current process, opens a pseudo-terminal, and makes the slave the
        /// controlling terminal of the child.  Returns the child PID to the parent (positive)
        /// and 0 to the child.  On failure returns -1.
        /// </summary>
        public static int ForkPty(out int masterFd, ref WinSize winsize)
        {
            if (OperatingSystem.IsMacOS())
                return ForkPtyMacOS(out masterFd, IntPtr.Zero, IntPtr.Zero, ref winsize);

            return ForkPtyLinux(out masterFd, IntPtr.Zero, IntPtr.Zero, ref winsize);
        }

        // ── execvp ────────────────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "execvp", SetLastError = true)]
        public static extern int ExecVp(string file, string?[] argv);

        // ── chdir ─────────────────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "chdir", SetLastError = true)]
        public static extern int Chdir(string path);

        // ── _exit ─────────────────────────────────────────────────────────────────
        // Used in child after fork to avoid running atexit handlers or flushing buffers.

        [DllImport("libc", EntryPoint = "_exit")]
        public static extern void Exit(int status);

        // ── ioctl/TIOCSWINSZ ──────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        public static extern int Ioctl(int fd, nuint request, ref WinSize winsize);

        // The numeric value of TIOCSWINSZ differs between Linux and macOS.
        public static nuint TIOCSWINSZ => OperatingSystem.IsMacOS() ? 0x80087467u : 0x5414u;

        // ── close ─────────────────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        public static extern int Close(int fd);

        // ── fcntl / FD_CLOEXEC ────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
        public static extern int Fcntl(int fd, int cmd, int arg);

        // F_SETFD = 2, FD_CLOEXEC = 1 — standard POSIX values
        public const int F_SETFD = 2;
        public const int FD_CLOEXEC = 1;

        // ── read / write ──────────────────────────────────────────────────────────

        [DllImport("libc", EntryPoint = "read", SetLastError = true)]
        public static extern nint Read(int fd, ref byte buf, nuint count);

        [DllImport("libc", EntryPoint = "write", SetLastError = true)]
        public static extern nint Write(int fd, ref byte buf, nuint count);

        // errno constants
        public const int EINTR = 4;
        public const int EIO = 5; // returned when PTY slave side is closed
    }

    public static partial class Windows
    {
        // ── Pseudo-console ────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CreatePseudoConsole(
            Coord size,
            Microsoft.Win32.SafeHandles.SafeFileHandle hInput,
            Microsoft.Win32.SafeHandles.SafeFileHandle hOutput,
            uint dwFlags,
            out IntPtr phPC
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void ClosePseudoConsole(IntPtr hPC);

        // ── Pipes ─────────────────────────────────────────────────────────────────

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreatePipe(
            out Microsoft.Win32.SafeHandles.SafeFileHandle hReadPipe,
            out Microsoft.Win32.SafeHandles.SafeFileHandle hWritePipe,
            IntPtr lpPipeAttributes,
            uint nSize
        );

        // ── Process attribute list ────────────────────────────────────────────────

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool InitializeProcThreadAttributeList(
            IntPtr lpAttributeList,
            int dwAttributeCount,
            int dwFlags,
            ref IntPtr lpSize
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool UpdateProcThreadAttribute(
            IntPtr lpAttributeList,
            uint dwFlags,
            IntPtr attribute,
            IntPtr lpValue,
            IntPtr cbSize,
            IntPtr lpPreviousValue,
            IntPtr lpReturnSize
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

        // ── CreateProcessW ────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct StartupInfo
        {
            public int cb;
            public string? lpReserved;
            public string? lpDesktop;
            public string? lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct StartupInfoEx
        {
            public StartupInfo StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateProcessW(
            string? lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref StartupInfoEx lpStartupInfo,
            out ProcessInformation lpProcessInformation
        );

        // ── Handle helpers ────────────────────────────────────────────────────────

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        // ── Constants ─────────────────────────────────────────────────────────────

        public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const int STARTF_USESTDHANDLES = 0x00000100;
        public static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x00020016;
        public const uint INFINITE = 0xFFFFFFFF;
    }
}
