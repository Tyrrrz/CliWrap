using System;
using System.Runtime.InteropServices;

namespace CliWrap.Utils;

internal static partial class NativeMethods
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

        // Signal numbers (POSIX standard)
        public const int SIGINT = 2;
        public const int SIGKILL = 9;

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
}
