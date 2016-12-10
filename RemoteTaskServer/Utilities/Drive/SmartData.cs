#region

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace UlteriusServer.Utilities.Drive
{
    public class SmartData
    {
        public SmartData(byte[] arrVendorSpecific, byte[] arrThreshold)
        {
            _attributes = new Dictionary<SmartAttributeType, SmartAttribute>();

            for (var offset = 2; offset < arrVendorSpecific.Length;)
            {
                var a = FromBytes<SmartAttribute>(arrVendorSpecific, ref offset, 12);
                a.Threshold = arrThreshold[offset - 12 + 1];
                a.VendorData[a.VendorData.Length - 1] = 0;
                /* Attribute values 0x00, 0xfe, 0xff are invalid */
                if (a.AttributeType != 0x00 && (byte)a.AttributeType != 0xfe && (byte)a.AttributeType != 0xff)
                {
                    _attributes[a.AttributeType] = a;
                }
            }

            StructureVersion = (ushort)(arrVendorSpecific[0] * 256 + arrVendorSpecific[1]);
        }

        #region Private methods

        private static T FromBytes<T>(byte[] bytearray, ref int offset, int count)
        {
            var ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.AllocHGlobal(count);
                Marshal.Copy(bytearray, offset, ptr, count);
                offset += count;
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        #endregion

        #region Struct

        [StructLayout(LayoutKind.Sequential)]
        public struct SmartAttribute
        {
            public SmartAttributeType AttributeType;
            public ushort Flags;
            public byte Value;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] VendorData;

            public bool Advisory => (Flags & 0x1) == 0x0;

            public bool FailureImminent => (Flags & 0x1) == 0x1;

            public bool OnlineDataCollection => (Flags & 0x2) == 0x2;
            public byte Threshold;
        }

        #endregion

        #region Fields

        private readonly Dictionary<SmartAttributeType, SmartAttribute> _attributes;

        public IEnumerable<SmartAttribute> Attributes => _attributes.Values;

        public ushort StructureVersion { get; }

        public SmartAttribute this[SmartAttributeType v] => _attributes[v];

        #endregion
    }
}