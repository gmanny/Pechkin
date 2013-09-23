using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pechkin.Util
{
    /// <summary>
    /// Marshaller for UTF8 strings.
    /// </summary>
    public class Utf8Marshaler : ICustomMarshaler
    {
        static Utf8Marshaler _staticInstance;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException(
                       "UTF8Marshaler must be used on a string.");

            // not null terminated
            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);

            // write the terminating null
            Marshal.WriteByte(new IntPtr(buffer.ToInt64() + (long)strbuf.Length), 0);
            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            byte* walk = (byte*)pNativeData;

            // find the end of the string
            while (*walk != 0)
            {
                walk++;
            }
            int length = (int)(walk - (byte*)pNativeData);

            // should not be null terminated
            byte[] strbuf = new byte[length - 1];
            // skip the trailing null
            Marshal.Copy(pNativeData, strbuf, 0, length - 1);
            string data = Encoding.UTF8.GetString(strbuf);
            return data;
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        public void CleanUpManagedData(object managedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (_staticInstance == null)
            {
                return _staticInstance = new Utf8Marshaler();
            }
            return _staticInstance;
        }
    }
}
