#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UlteriusServer.Api.Network.Models;

#endregion

namespace UlteriusServer.Api.Win32
{
    internal class Display
    {
        [Flags]
        public enum ChangeDisplaySettingsFlags : uint
        {
            CDS_NONE = 0,
            CDS_UPDATEREGISTRY = 0x00000001,
            CDS_TEST = 0x00000002,
            CDS_FULLSCREEN = 0x00000004,
            CDS_GLOBAL = 0x00000008,
            CDS_SET_PRIMARY = 0x00000010,
            CDS_VIDEOPARAMETERS = 0x00000020,
            CDS_ENABLE_UNSAFE_MODES = 0x00000100,
            CDS_DISABLE_UNSAFE_MODES = 0x00000200,
            CDS_RESET = 0x40000000,
            CDS_RESET_EX = 0x20000000,
            CDS_NORESET = 0x10000000
        }
        public const int DMDO_DEFAULT = 0;
        public const int DMDO_90 = 1;
        public const int DMDO_180 = 2;
        public const int DMDO_270 = 3;
        [Flags]
        public enum DisplayDeviceStateFlags
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,

            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,

            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,

            /// <summary>The device is VGA compatible.</summary>
            VgaCompatible = 0x10,

            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,

            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        public const int ErrorSuccess = 0;

        public const int ENUM_CURRENT_SETTINGS = -1;
        public const int CDS_UPDATEREGISTRY = 0x01;
        public const int CDS_TEST = 0x02;


        private const int ENUM_REGISTRY_SETTINGS = -2;

        public static int EnumCurrentSettings { get; } = -1;

        public static int EnumRegistrySettings { get; } = -2;

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
                throw new Win32Exception(error);
            return deviceName.monitorFriendlyDeviceName;
        }

        private static IEnumerable<string> GetAllMonitorsFriendlyNames()
        {
            uint pathCount, modeCount;
            var error = GetDisplayConfigBufferSizes(QueryDeviceConfigFlags.QdcOnlyActivePaths, out pathCount,
                out modeCount);
            if (error != ErrorSuccess)
                throw new Win32Exception(error);

            var displayPaths = new DisplayconfigPathInfo[pathCount];
            var displayModes = new DisplayconfigModeInfo[modeCount];
            error = QueryDisplayConfig(QueryDeviceConfigFlags.QdcOnlyActivePaths,
                ref pathCount, displayPaths, ref modeCount, displayModes, IntPtr.Zero);
            if (error != ErrorSuccess)
                throw new Win32Exception(error);

            for (var i = 0; i < modeCount; i++)
                if (displayModes[i].infoType == DisplayconfigModeInfoType.DisplayconfigModeInfoTypeTarget)
                    yield return MonitorFriendlyName(displayModes[i].adapterId, displayModes[i].id);
        }

        public static List<string> DeviceFriendlyName()
        {
            return GetAllMonitorsFriendlyNames().ToList();
        }

        [DllImport("user32.dll")]
        public static extern int ChangeDisplaySettings(
            ref Devmode devMode, int flags);

        [DllImport("user32.dll")]
        public static extern DISP_CHANGE ChangeDisplaySettingsEx(string lpszDeviceName, ref Devmode lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern DISP_CHANGE ChangeDisplaySettingsEx(string lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, ChangeDisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(
            string deviceName, int modeNum, ref Devmode devMode);


        public static List<DisplayInformation> DisplayInformation()
        {
            var monitors = new List<DisplayInformation>();
            var d = new DisplayDevice();
            d.cb = Marshal.SizeOf(d);
            try
            {
                for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
                {
                    if (d.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop))
                    {
                        var device = d.DeviceName;

                        var vDevMode = new Devmode();
                        var i = 0;
                        var supportedResolutions = new Dictionary<string, List<ResolutionInformation>>();
                        while (EnumDisplaySettings(device, i, ref vDevMode))
                        {
                            var width = vDevMode.dmPelsWidth;
                            var height = vDevMode.dmPelsHeight;
                            var bpp = vDevMode.dmBitsPerPel;
                            var orientation = vDevMode.dmDisplayOrientation.ToString();
                            var freq = vDevMode.dmDisplayFrequency;
                            var resolutionKey = $"{width}x{height}";
                            var resolution = new ResolutionInformation
                            {
                                BitsPerPixel = bpp,
                                Frequency = freq,
                                Height = height,
                                Width = width,
                                Orientation = orientation
                            };
                            if (supportedResolutions.ContainsKey(resolutionKey))
                            {
                                supportedResolutions[resolutionKey].Add(resolution);
                            }
                            else
                            {
                                supportedResolutions.Add(resolutionKey, new List<ResolutionInformation>());
                                supportedResolutions[resolutionKey].Add(resolution);
                            }
                            i++;
                        }
                        var cDevMode = new Devmode();
                        EnumDisplaySettings(device, ENUM_CURRENT_SETTINGS, ref cDevMode);
                      
                        var currentResolution = new ResolutionInformation
                        {
                            BitsPerPixel = cDevMode.dmBitsPerPel,
                            Frequency = cDevMode.dmDisplayFrequency,
                            Height = cDevMode.dmPelsHeight,
                            Width = cDevMode.dmPelsWidth,
                            Orientation = cDevMode.dmDisplayOrientation.ToString()
                        };
                        var monitor = new DisplayInformation
                        {
                            Primary = d.StateFlags.HasFlag(DisplayDeviceStateFlags.PrimaryDevice),
                            Attached = d.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop),
                            Removable = d.StateFlags.HasFlag(DisplayDeviceStateFlags.Removable),
                            VgaCompatible = d.StateFlags.HasFlag(DisplayDeviceStateFlags.VgaCompatible),
                            MirroringDriver = d.StateFlags.HasFlag(DisplayDeviceStateFlags.MirroringDriver),
                            MultiDriver = d.StateFlags.HasFlag(DisplayDeviceStateFlags.MultiDriver),
                            ModesPruned = d.StateFlags.HasFlag(DisplayDeviceStateFlags.ModesPruned),
                            Remote = d.StateFlags.HasFlag(DisplayDeviceStateFlags.Remote),
                            Disconnect = d.StateFlags.HasFlag(DisplayDeviceStateFlags.Disconnect),
                            FriendlyName = $"{GetAllMonitorsFriendlyNames().ElementAt((int) id)} on {d.DeviceString}",
                            SupportedResolutions = supportedResolutions,
                            CurrentResolution = currentResolution,
                            DeviceName = device
                        };
                        monitors.Add(monitor);
                        d.cb = Marshal.SizeOf(d);
                        EnumDisplayDevices(d.DeviceName, 0, ref d, 0);
                    }
                    d.cb = Marshal.SizeOf(d);
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"{ex}");
                }
            }
            return monitors;
        }
        public static string SetPrimary(string deviceName)
        {
            var id = int.Parse(Regex.Match(deviceName, @"\d+").Value) - 1;
            var originalMode = new Devmode();
            originalMode.dmSize = (short)Marshal.SizeOf(originalMode);
            EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref originalMode);
            var offsetx = originalMode.dmPositionX;
            var offsety = originalMode.dmPositionY;
            originalMode.dmPositionX = 0;
            originalMode.dmPositionY = 0;
          
            ChangeDisplaySettingsEx(deviceName, ref originalMode, (IntPtr)null, (ChangeDisplaySettingsFlags.CDS_SET_PRIMARY | ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY | ChangeDisplaySettingsFlags.CDS_NORESET), IntPtr.Zero);
            var device = new DisplayDevice();
            device.cb = Marshal.SizeOf(device);

            // Update remaining devices
            for (uint otherid = 0; EnumDisplayDevices(null, otherid, ref device, 0); otherid++)
            {
                if (device.StateFlags.HasFlag(DisplayDeviceStateFlags.AttachedToDesktop) && otherid != id)
                {
                    device.cb = Marshal.SizeOf(device);
                    var otherDeviceMode = new Devmode();

                   EnumDisplaySettings(device.DeviceName, -1, ref otherDeviceMode);

                    otherDeviceMode.dmPositionX -= offsetx;
                    otherDeviceMode.dmPositionY -= offsety;

                    ChangeDisplaySettingsEx(
                        device.DeviceName,
                        ref otherDeviceMode,
                        (IntPtr)null,
                        (ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY | ChangeDisplaySettingsFlags.CDS_NORESET),
                        IntPtr.Zero);

                }

                device.cb = Marshal.SizeOf(device);
            }

            // Apply settings
           return GetMessageForCode(ChangeDisplaySettingsEx(null, IntPtr.Zero, (IntPtr)null, ChangeDisplaySettingsFlags.CDS_NONE, (IntPtr)null));
        }

        public static string Rotate(int angle, int width, int height, string deviceName)
        {
            var originalMode = new Devmode();
            originalMode.dmSize = (short)Marshal.SizeOf(originalMode);
            EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref originalMode);

            // swap height and width
            int temp = originalMode.dmPelsHeight;
            originalMode.dmPelsHeight = originalMode.dmPelsWidth;
            originalMode.dmPelsWidth = temp;

            originalMode.dmPelsWidth = width;
            originalMode.dmPelsHeight = height;
            switch (angle)
            {
                case 0:
                    originalMode.dmDisplayOrientation = ScreenOrientation.Angle0;
                    break;
                case 90:
                    originalMode.dmDisplayOrientation = ScreenOrientation.Angle90;
                    break;
                case 180:
                    originalMode.dmDisplayOrientation = ScreenOrientation.Angle180;
                    break;
                case 270:
                    originalMode.dmDisplayOrientation =ScreenOrientation.Angle270;
                    break;
            }
           return GetMessageForCode(ChangeDisplaySettingsEx(deviceName, ref originalMode, IntPtr.Zero,
               ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero));
        }

        public static string ChangeResolution(string deviceName, int width, int height, int bbp, int freq)
        {
            var originalMode = new Devmode();
            originalMode.dmSize = (short) Marshal.SizeOf(originalMode);
            EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref originalMode);
            var newMode = originalMode;
            newMode.dmDeviceName = deviceName;
            newMode.dmPelsWidth = width;
            newMode.dmPelsHeight = height;
            newMode.dmBitsPerPel = bbp;
            newMode.dmDisplayFrequency = freq;
            return GetMessageForCode(ChangeDisplaySettingsEx(deviceName, ref newMode, IntPtr.Zero,
                ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero));
        }

        private static string GetMessageForCode(DISP_CHANGE code)
        {
            string message;
            switch (code)
            {
                case Display.DISP_CHANGE.Successful:
                    message = "Resolution updated.";
                    break;
                case Display.DISP_CHANGE.Restart:
                    message = "A restart is required for this resolution to take effect.";
                    break;
                case Display.DISP_CHANGE.BadMode:
                    message = $"resolution is not valid.";
                    break;
                case Display.DISP_CHANGE.BadDualView:
                    message = "The settings change was unsuccessful because system is DualView capable.";
                    break;
                case Display.DISP_CHANGE.BadFlags:
                    message = "An invalid set of flags was passed in.";
                    break;
                case Display.DISP_CHANGE.BadParam:
                    message = "An invalid parameter was passed in. This can include an invalid flag or combination of flags.";
                    break;
                case Display.DISP_CHANGE.Failed:
                    message = "Resolution failed to update.";
                    break;
                case Display.DISP_CHANGE.NotUpdated:
                    message = "Unable to write settings to the registry.";
                    break;
                default:
                    message = "Unknown return value from ChangeDisplaySettings API.";
                    break;
            }
            return message;
        }
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice,
            uint dwFlags);

        internal enum DISP_CHANGE
        {
            Successful = 0,
            Restart = 1,
            Failed = -1,
            BadMode = -2,
            NotUpdated = -3,
            BadFlags = -4,
            BadParam = -5,
            BadDualView = -6
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Devmode
        {
            private const int Cchdevicename = 0x20;
            private const int Cchformname = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)] public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            [MarshalAs(UnmanagedType.U4)] public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        #region enums

        public enum QueryDeviceConfigFlags : uint
        {
            QdcAllPaths = 0x00000001,
            QdcOnlyActivePaths = 0x00000002,
            QdcDatabaseCurrent = 0x00000004
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

        public enum DisplayconfigScanlineOrdering : uint
        {
            DisplayconfigScanlineOrderingUnspecified = 0,
            DisplayconfigScanlineOrderingProgressive = 1,
            DisplayconfigScanlineOrderingInterlaced = 2,
            DisplayconfigScanlineOrderingInterlacedUpperfieldfirst = DisplayconfigScanlineOrderingInterlaced,
            DisplayconfigScanlineOrderingInterlacedLowerfieldfirst = 3,
            DisplayconfigScanlineOrderingForceUint32 = 0xFFFFFFFF
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

        public enum DisplayconfigPixelformat : uint
        {
            DisplayconfigPixelformat8Bpp = 1,
            DisplayconfigPixelformat16Bpp = 2,
            DisplayconfigPixelformat24Bpp = 3,
            DisplayconfigPixelformat32Bpp = 4,
            DisplayconfigPixelformatNongdi = 5,
            DisplayconfigPixelformatForceUint32 = 0xffffffff
        }

        public enum DisplayconfigModeInfoType : uint
        {
            DisplayconfigModeInfoTypeSource = 1,
            DisplayconfigModeInfoTypeTarget = 2,
            DisplayconfigModeInfoTypeForceUint32 = 0xFFFFFFFF
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

        #endregion

        #region structs

        [StructLayout(LayoutKind.Sequential)]
        public struct Luid
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigPathSourceInfo
        {
            public Luid adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
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
        public struct DisplayconfigRational
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigPathInfo
        {
            public DisplayconfigPathSourceInfo sourceInfo;
            public DisplayconfigPathTargetInfo targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Displayconfig2Dregion
        {
            public uint cx;
            public uint cy;
        }

        [StructLayout(LayoutKind.Sequential)]
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
        public struct DisplayconfigTargetMode
        {
            public DisplayconfigVideoSignalInfo targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Pointl
        {
            private readonly int x;
            private readonly int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigSourceMode
        {
            public uint width;
            public uint height;
            public DisplayconfigPixelformat pixelFormat;
            public Pointl position;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DisplayconfigModeInfoUnion
        {
            [FieldOffset(0)] public DisplayconfigTargetMode targetMode;

            [FieldOffset(0)] public DisplayconfigSourceMode sourceMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayconfigModeInfo
        {
            public DisplayconfigModeInfoType infoType;
            public uint id;
            public Luid adapterId;
            public DisplayconfigModeInfoUnion modeInfo;
        }

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

        #endregion

        #region DLL-Imports

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

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DisplayconfigTargetDeviceName deviceName);

        #endregion

       
    }
}