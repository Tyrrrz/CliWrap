using System;
using System.Runtime.InteropServices;

namespace CliWrap.Utils
{
    /// <summary>
    /// DLL imports for windows-specific APIs.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Returns a pointer to the console window.
        /// </summary>
        /// <returns>Pointer to the console window, IntPtr.Zero if there is no console.</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
    }
}
