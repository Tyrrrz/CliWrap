using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace CliWrap;

internal class SecureStringHelper
{
    internal static string? MarshalToString(SecureString input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        if (input.Length == 0)
        {
            return string.Empty;
        }
        var ptr = IntPtr.Zero;
        string? result;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(input);
            result = Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
        return result;
    }

    internal static unsafe SecureString MarshalToSecureString(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new SecureString();
        }
        fixed (char* ptr = input)
        {
            return new SecureString(ptr, input.Length);
        }
    }
}