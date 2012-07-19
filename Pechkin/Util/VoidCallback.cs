using System;
using System.Runtime.InteropServices;

namespace Pechkin.Util
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VoidCallback(IntPtr converter);
}