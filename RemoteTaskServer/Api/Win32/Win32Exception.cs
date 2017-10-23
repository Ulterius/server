#region

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;

#endregion

namespace UlteriusServer.Api.Win32
{
    [SuppressUnmanagedCodeSecurity]
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    [Serializable]
    internal class Win32Exception : ExternalException
    {
        // Microsoft.Win32.NativeMethods
        public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Win32Exception() : this(Marshal.GetLastWin32Error())
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Win32Exception(int error) : this(error, GetErrorMessage(error))
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Win32Exception(int error, string message) : base(message)
        {
            NativeErrorCode = error;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Win32Exception(string message) : this(Marshal.GetLastWin32Error(), message)
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public Win32Exception(string message, Exception innerException) : base(message, innerException)
        {
            NativeErrorCode = Marshal.GetLastWin32Error();
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected Win32Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            //IntSecurity.UnmanagedCode.Demand();
            NativeErrorCode = info.GetInt32("NativeErrorCode");
        }

        public int NativeErrorCode { get; }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("NativeErrorCode", NativeErrorCode);
            base.GetObjectData(info, context);
        }

        // Microsoft.Win32.SafeNativeMethods
        [DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId,
            StringBuilder lpBuffer, int nSize, IntPtr arguments);

        private static string GetErrorMessage(int error)
        {
            string result;
            var stringBuilder = new StringBuilder(256);
            var num = FormatMessage(12800, NullHandleRef, error, 0, stringBuilder, stringBuilder.Capacity + 1,
                IntPtr.Zero);
            if (num != 0)
            {
                int i;
                for (i = stringBuilder.Length; i > 0; i--)
                {
                    var c = stringBuilder[i - 1];
                    if (c > ' ' && c != '.')
                    {
                        break;
                    }
                }
                result = stringBuilder.ToString(0, i);
            }
            else
            {
                result = "Unknown error (0x" + Convert.ToString(error, 16) + ")";
            }
            return result;
        }
    }
}