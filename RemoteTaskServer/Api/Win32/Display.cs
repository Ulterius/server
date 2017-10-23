#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

#endregion

namespace UlteriusServer.Api.Win32
{
    public class Monitor
    {
        public Rectangle Rect { get; set; }
        public Rectangle WorkRect { get; set; }
        public bool Primary { get; set; }
        public string DeviceName { get; set; }
    }

    public class Display
    {

       
        private static IEnumerable<string> GetAllMonitorsFriendlyNames()
        {
            uint pathCount, modeCount;
            var error = GetDisplayConfigBufferSizes(QueryDeviceConfigFlags.QdcOnlyActivePaths, out pathCount,
                out modeCount);
            if (error != ErrorSuccess)
                throw new System.ComponentModel.Win32Exception(error);

            var displayPaths = new DisplayconfigPathInfo[pathCount];
            var displayModes = new DisplayconfigModeInfo[modeCount];
            error = QueryDisplayConfig(QueryDeviceConfigFlags.QdcOnlyActivePaths,
                ref pathCount, displayPaths, ref modeCount, displayModes, IntPtr.Zero);
            if (error != ErrorSuccess)
                throw new System.ComponentModel.Win32Exception(error);

            for (var i = 0; i < modeCount; i++)
                if (displayModes[i].infoType == DisplayconfigModeInfoType.DisplayconfigModeInfoTypeTarget)
                    yield return MonitorFriendlyName(displayModes[i].adapterId, displayModes[i].id);
        }

        public static string GetDeviceFriendlyName(int screenIndex)
        {
            var allFriendlyNames = GetAllMonitorsFriendlyNames().ToList();
            if (screenIndex > allFriendlyNames.Count)
            {
                return null;
            }
            return allFriendlyNames.ElementAtOrDefault(screenIndex) ?? "Unknown";
        }

        private static string MonitorFriendlyName(Luid adapterId, uint targetId)
        {
            var deviceName = new DisplayconfigTargetDeviceName
            {
                header =
                {
                    size = (uint) Marshal.SizeOf(typeof(DisplayconfigTargetDeviceName)),
                    adapterId = adapterId,
                    id = targetId,
                    type = DisplayconfigDeviceInfoType.DisplayconfigDeviceInfoGetTargetName
                }
            };
            var error = DisplayConfigGetDeviceInfo(ref deviceName);
            if (error != ErrorSuccess)
                throw new System.ComponentModel.Win32Exception(error);
            return deviceName.monitorFriendlyDeviceName;
        }

        public const int ErrorSuccess = 0;
        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(
            QueryDeviceConfigFlags flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(
            QueryDeviceConfigFlags flags,
            ref uint numPathArrayElements, [Out] DisplayconfigPathInfo[] pathInfoArray,
            ref uint numModeInfoArrayElements, [Out] DisplayconfigModeInfo[] modeInfoArray,
            IntPtr currentTopologyId
        );
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigTargetDeviceNameFlags
        {
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigDeviceInfoHeader
        {
            public DisplayconfigDeviceInfoType type;
            public uint size;
            public Luid adapterId;
            public uint id;
        }

        public enum DisplayconfigDeviceInfoType : uint
        {
            DisplayconfigDeviceInfoGetSourceName = 1,
            DisplayconfigDeviceInfoGetTargetName = 2,
            DisplayconfigDeviceInfoGetTargetPreferredMode = 3,
            DisplayconfigDeviceInfoGetAdapterName = 4,
            DisplayconfigDeviceInfoSetTargetPersistence = 5,
            DisplayconfigDeviceInfoGetTargetBaseType = 6,
            DisplayconfigDeviceInfoForceUint32 = 0xFFFFFFFF
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DisplayconfigTargetDeviceName
        {
            public DisplayconfigDeviceInfoHeader header;
            public DisplayconfigTargetDeviceNameFlags flags;
            public DisplayconfigVideoOutputTechnology outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigModeInfo
        {
            public DisplayconfigModeInfoType infoType;
            public uint id;
            public Luid adapterId;
            public DisplayconfigModeInfoUnion modeInfo;
        }
        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DisplayconfigTargetDeviceName deviceName);

        public enum QueryDeviceConfigFlags : uint
        {
            QdcAllPaths = 0x00000001,
            QdcOnlyActivePaths = 0x00000002,
            QdcDatabaseCurrent = 0x00000004
        }
        public enum DisplayconfigModeInfoType : uint
        {
            DisplayconfigModeInfoTypeSource = 1,
            DisplayconfigModeInfoTypeTarget = 2,
            DisplayconfigModeInfoTypeForceUint32 = 0xFFFFFFFF
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct DisplayconfigModeInfoUnion
        {
            [FieldOffset(0)] public DisplayconfigTargetMode targetMode;

            [FieldOffset(0)] public DisplayconfigSourceMode sourceMode;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigTargetMode
        {
            public DisplayconfigVideoSignalInfo targetVideoSignalInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigPathInfo
        {
            public DisplayconfigPathSourceInfo sourceInfo;
            public DisplayconfigPathTargetInfo targetInfo;
            public uint flags;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigSourceMode
        {
            public uint width;
            public uint height;
            public DisplayconfigPixelformat pixelFormat;
            public Pointl position;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigPathSourceInfo
        {
            public Luid adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        public static Rectangle GetWindowRectangle()
        {
            var scBounds = new RECT();
            GetWindowRect(GetDesktopWindow(), ref scBounds);
            return scBounds;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);



        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom)
            {
            }

            public int X
            {
                get => Left;
                set
                {
                    Right -= Left - value;
                    Left = value;
                }
            }

            public int Y
            {
                get => Top;
                set
                {
                    Bottom -= Top - value;
                    Top = value;
                }
            }

            public int Height
            {
                get => Bottom - Top;
                set => Bottom = value + Top;
            }

            public int Width
            {
                get => Right - Left;
                set => Right = value + Left;
            }


            public Size Size
            {
                get => new Size(Width, Height);
                set
                {
                    Width = value.Width;
                    Height = value.Height;
                }
            }

            public static implicit operator Rectangle(RECT r)
            {
                return new Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                if (obj is Rectangle)
                    return Equals(new RECT((Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top,
                    Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Displayconfig2Dregion
        {
            public uint cx;
            public uint cy;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Pointl
        {
            private readonly int x;
            private readonly int y;
        }
        public enum DisplayconfigPixelformat : uint
        {
            DisplayconfigPixelformat8Bpp = 1,
            DisplayconfigPixelformat16Bpp = 2,
            DisplayconfigPixelformat24Bpp = 3,
            DisplayconfigPixelformat32Bpp = 4,
            DisplayconfigPixelformatNongdi = 5,
            DisplayconfigPixelformatForceUint32 = 0xffffffff
        }
        public struct DisplayconfigVideoSignalInfo
        {
            public ulong pixelRate;
            public DisplayconfigRational hSyncFreq;
            public DisplayconfigRational vSyncFreq;
            public Displayconfig2Dregion activeSize;
            public Displayconfig2Dregion totalSize;
            public uint videoStandard;
            public DisplayconfigScanlineOrdering scanLineOrdering;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigPathTargetInfo
        {
            public Luid adapterId;
            public uint id;
            public uint modeInfoIdx;
            private readonly DisplayconfigVideoOutputTechnology outputTechnology;
            private readonly DisplayconfigRotation rotation;
            private readonly DisplayconfigScaling scaling;
            private readonly DisplayconfigRational refreshRate;
            private readonly DisplayconfigScanlineOrdering scanLineOrdering;
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Luid
        {
            public uint LowPart;
            public int HighPart;
        }


        public enum DisplayconfigScanlineOrdering : uint
        {
            DisplayconfigScanlineOrderingUnspecified = 0,
            DisplayconfigScanlineOrderingProgressive = 1,
            DisplayconfigScanlineOrderingInterlaced = 2,
            DisplayconfigScanlineOrderingInterlacedUpperfieldfirst = DisplayconfigScanlineOrderingInterlaced,
            DisplayconfigScanlineOrderingInterlacedLowerfieldfirst = 3,
            DisplayconfigScanlineOrderingForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayconfigVideoOutputTechnology : uint
        {
            DisplayconfigOutputTechnologyOther = 0xFFFFFFFF,
            DisplayconfigOutputTechnologyHd15 = 0,
            DisplayconfigOutputTechnologySvideo = 1,
            DisplayconfigOutputTechnologyCompositeVideo = 2,
            DisplayconfigOutputTechnologyComponentVideo = 3,
            DisplayconfigOutputTechnologyDvi = 4,
            DisplayconfigOutputTechnologyHdmi = 5,
            DisplayconfigOutputTechnologyLvds = 6,
            DisplayconfigOutputTechnologyDJpn = 8,
            DisplayconfigOutputTechnologySdi = 9,
            DisplayconfigOutputTechnologyDisplayportExternal = 10,
            DisplayconfigOutputTechnologyDisplayportEmbedded = 11,
            DisplayconfigOutputTechnologyUdiExternal = 12,
            DisplayconfigOutputTechnologyUdiEmbedded = 13,
            DisplayconfigOutputTechnologySdtvdongle = 14,
            DisplayconfigOutputTechnologyMiracast = 15,
            DisplayconfigOutputTechnologyInternal = 0x80000000,
            DisplayconfigOutputTechnologyForceUint32 = 0xFFFFFFFF
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigRational
        {
            public uint Numerator;
            public uint Denominator;
        }

        public enum DisplayconfigRotation : uint
        {
            DisplayconfigRotationIdentity = 1,
            DisplayconfigRotationRotate90 = 2,
            DisplayconfigRotationRotate180 = 3,
            DisplayconfigRotationRotate270 = 4,
            DisplayconfigRotationForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayconfigScaling : uint
        {
            DisplayconfigScalingIdentity = 1,
            DisplayconfigScalingCentered = 2,
            DisplayconfigScalingStretched = 3,
            DisplayconfigScalingAspectratiocenteredmax = 4,
            DisplayconfigScalingCustom = 5,
            DisplayconfigScalingPreferred = 128,
            DisplayconfigScalingForceUint32 = 0xFFFFFFFF
        }
        public const int MONITORINFOF_PRIMARY = 0x1;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        private static List<Monitor> GetMonitors()
        {
            List<Monitor> monitors = new List<Monitor>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    MONITORINFOEX monitorInfo = new MONITORINFOEX();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

                    GetMonitorInfo(hMonitor, ref monitorInfo);

                    monitors.Add(new Monitor()
                    {
                        Primary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0,
                        DeviceName = monitorInfo.szDevice,
                        Rect = Rectangle.FromLTRB(monitorInfo.rcMonitor.Left,
                            monitorInfo.rcMonitor.Top,
                            monitorInfo.rcMonitor.Right,
                            monitorInfo.rcMonitor.Bottom),
                        WorkRect = Rectangle.FromLTRB(monitorInfo.rcWork.Left,
                            monitorInfo.rcWork.Top,
                            monitorInfo.rcWork.Right,
                            monitorInfo.rcWork.Bottom)
                    });

                    return true;
                },
                IntPtr.Zero
            );

            return monitors;
        }
        public static int X { get; set; }

        public static int Y { get; set; }

        public static int Height { get; set; }

        public static int Width { get; set; }
    }
}