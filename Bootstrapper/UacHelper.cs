#region

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

#endregion

namespace Bootstrapper
{
    public static class UacHelper
    {
        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private const string UacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string UacRegistryValue = "EnableLUA";

        private static readonly uint StandardRightsRead = 0x00020000;
        private static readonly uint TokenQuery = 0x0008;
        private static readonly uint TokenRead = StandardRightsRead | TokenQuery;

        public static bool IsUacEnabled
        {
            get
            {
                var uacKey = Registry.LocalMachine.OpenSubKey(UacRegistryKey, false);
                var result = uacKey.GetValue(UacRegistryValue).Equals(1);
                return result;
            }
        }

        public static bool IsProcessElevated
        {
            get
            {
                if (IsUacEnabled)
                {
                    IntPtr tokenHandle;
                    if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenRead, out tokenHandle))
                    {
                        throw new ApplicationException("Could not get process token.  Win32 Error Code: " +
                                                       Marshal.GetLastWin32Error());
                    }

                    var elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                    var elevationResultSize = Marshal.SizeOf((int) elevationResult);
                    uint returnedSize = 0;
                    var elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

                    var success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType,
                        elevationTypePtr, (uint) elevationResultSize, out returnedSize);
                    if (success)
                    {
                        elevationResult = (TOKEN_ELEVATION_TYPE) Marshal.ReadInt32(elevationTypePtr);
                        var isProcessAdmin = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                        return isProcessAdmin;
                    }
                    throw new ApplicationException("Unable to determine the current elevation.");
                }
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var result = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return result;
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass,
            IntPtr tokenInformation, uint tokenInformationLength, out uint returnLength);
    }
}