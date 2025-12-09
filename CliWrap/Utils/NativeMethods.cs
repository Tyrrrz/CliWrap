using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CliWrap.Utils;

internal static class NativeMethods
{
    public static class Unix
    {
        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int pid, int sig);

        [DllImport("libc", EntryPoint = "openpty", SetLastError = true)]
        public static extern int OpenPty(
            out int master,
            out int slave,
            IntPtr name,
            IntPtr termios,
            IntPtr winsize
        );

        [DllImport("libc", EntryPoint = "close", SetLastError = true)]
        public static extern int Close(int fd);

        [DllImport("libc", EntryPoint = "read", SetLastError = true)]
        public static extern nint Read(int fd, ref byte buf, nuint count);

        [DllImport("libc", EntryPoint = "write", SetLastError = true)]
        public static extern nint Write(int fd, ref byte buf, nuint count);

        // waitpid status macros
        public static bool WIFEXITED(int status) => (status & 0x7F) == 0;

        public static int WEXITSTATUS(int status) => (status >> 8) & 0xFF;

        public static bool WIFSIGNALED(int status) =>
            ((status & 0x7F) > 0) && ((status & 0x7F) != 0x7F);

        public static int WTERMSIG(int status) => status & 0x7F;

        // errno values
        public const int EINTR = 4;

        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        public static extern int Ioctl(int fd, nuint request, ref WinSize winSize);

        [DllImport("libc", EntryPoint = "setsid", SetLastError = true)]
        public static extern int SetSid();

        [DllImport("libc", EntryPoint = "dup2", SetLastError = true)]
        public static extern int Dup2(int oldfd, int newfd);

        [DllImport("libc", EntryPoint = "login_tty", SetLastError = true)]
        public static extern int LoginTty(int fd);

        [StructLayout(LayoutKind.Sequential)]
        public struct WinSize
        {
            public ushort Row;
            public ushort Col;
            public ushort XPixel;
            public ushort YPixel;
        }

        // TIOCSWINSZ ioctl request codes (platform-specific)
        public static nuint TIOCSWINSZ => OperatingSystem.IsMacOS() ? 0x80087467u : 0x5414u;

        // posix_spawn for proper PTY process creation
        [DllImport("libc", EntryPoint = "posix_spawnp", SetLastError = true)]
        public static extern int PosixSpawnp(
            out int pid,
            string file,
            IntPtr file_actions,
            IntPtr attrp,
            string[] argv,
            string[] envp
        );

        [DllImport("libc", EntryPoint = "posix_spawn_file_actions_init", SetLastError = true)]
        public static extern int PosixSpawnFileActionsInit(IntPtr file_actions);

        [DllImport("libc", EntryPoint = "posix_spawn_file_actions_destroy", SetLastError = true)]
        public static extern int PosixSpawnFileActionsDestroy(IntPtr file_actions);

        [DllImport("libc", EntryPoint = "posix_spawn_file_actions_adddup2", SetLastError = true)]
        public static extern int PosixSpawnFileActionsAddDup2(
            IntPtr file_actions,
            int fd,
            int newfd
        );

        [DllImport("libc", EntryPoint = "posix_spawn_file_actions_addclose", SetLastError = true)]
        public static extern int PosixSpawnFileActionsAddClose(IntPtr file_actions, int fd);

        [DllImport("libc", EntryPoint = "posix_spawnattr_init", SetLastError = true)]
        public static extern int PosixSpawnAttrInit(IntPtr attr);

        [DllImport("libc", EntryPoint = "posix_spawnattr_destroy", SetLastError = true)]
        public static extern int PosixSpawnAttrDestroy(IntPtr attr);

        [DllImport("libc", EntryPoint = "posix_spawnattr_setflags", SetLastError = true)]
        public static extern int PosixSpawnAttrSetFlags(IntPtr attr, short flags);

        [DllImport("libc", EntryPoint = "waitpid", SetLastError = true)]
        public static extern int WaitPid(int pid, out int status, int options);

        // posix_spawn flags
        public const short POSIX_SPAWN_SETSID = 0x80; // Linux-specific, creates new session

        // waitpid options
        public const int WNOHANG = 1;

        // Size of posix_spawn structures (platform-specific, using max observed size)
        // Linux: posix_spawn_file_actions_t is 80 bytes, posix_spawnattr_t is 336 bytes
        // macOS: These are pointers (8 bytes on 64-bit)
        public static int PosixSpawnFileActionsSize => OperatingSystem.IsMacOS() ? 8 : 80;
        public static int PosixSpawnAttrSize => OperatingSystem.IsMacOS() ? 8 : 336;
    }

    public static class Windows
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CreatePseudoConsole(
            Coord size,
            SafeFileHandle hInput,
            SafeFileHandle hOutput,
            uint dwFlags,
            out IntPtr phPC
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResizePseudoConsole(IntPtr hPC, Coord size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void ClosePseudoConsole(IntPtr hPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreatePipe(
            out SafeFileHandle hReadPipe,
            out SafeFileHandle hWritePipe,
            IntPtr lpPipeAttributes,
            uint nSize
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetHandleInformation(
            SafeHandle hObject,
            uint dwMask,
            uint dwFlags
        );

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

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecurityAttributes
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct StartupInfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
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

        // Constants
        public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public const uint CREATE_NEW_PROCESS_GROUP = 0x00000200;
        public const int STARTF_USESTDHANDLES = 0x00000100;
        public static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x00020016;
        public const uint HANDLE_FLAG_INHERIT = 0x00000001;
        public const uint INFINITE = 0xFFFFFFFF;
        public const uint STILL_ACTIVE = 259;
    }
}
