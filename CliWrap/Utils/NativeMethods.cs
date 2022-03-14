using System;
using System.Runtime.InteropServices;

namespace CliWrap.Utils;

internal static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();
}
