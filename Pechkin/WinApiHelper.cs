using System;
using System.Runtime.InteropServices;
using Pechkin.Util;

namespace Pechkin
{
    internal static class WinApiHelper
    {
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
                                                String filename);
    }
}