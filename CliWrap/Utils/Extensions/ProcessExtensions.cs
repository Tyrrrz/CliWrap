using System;
using System.Diagnostics;
using System.IO;
using CliWrap.Native;

namespace CliWrap.Utils.Extensions;

internal static class ProcessExtensions
{
    extension(Process process)
    {
        public string FileName => Path.GetFileName(process.StartInfo.FileName);

        // Sends SIGINT
        public bool TryInterrupt()
        {
            try
            {
                // On Windows, we need to launch an external executable that will attach
                // to the target process's console and then send a Ctrl+C event to it.
                // https://github.com/Tyrrrz/CliWrap/issues/47
                if (OperatingSystem.IsWindows())
                {
                    using var signaler = WindowsSignaler.Deploy();
                    return signaler.TrySend(process.Id, 0);
                }

                // On Unix, we can just send the signal to the process directly
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    return NativeMethods.Unix.Kill(process.Id, 2) == 0;
                }

                // Unsupported platform
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Sends SIGKILL
        public bool TryKill(bool entireProcessTree = true)
        {
            try
            {
                process.Kill(entireProcessTree);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
