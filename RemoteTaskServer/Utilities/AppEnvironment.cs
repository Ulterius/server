#region

using System;
using System.Windows.Forms;
using Microsoft.Win32;

#endregion

namespace UlteriusServer.Utilities
{
    public class AppEnvironment
    {
        public static string DataPath
        {
            get
            {
                try
                {
                    // No version!
                    return Environment.GetEnvironmentVariable("AppData").Trim() + "\\" + Application.CompanyName + "\\" +
                           Application.ProductName;
                }
                catch
                {
                    // ignored
                }

                try
                {
                    // Version, but chopped out
                    return Application.UserAppDataPath.Substring(0,
                        Application.UserAppDataPath.LastIndexOf("\\", StringComparison.Ordinal));
                }
                catch
                {
                    try
                    {
                        // App launch folder
                        return Application.ExecutablePath.Substring(0,
                            Application.ExecutablePath.LastIndexOf("\\", StringComparison.Ordinal));
                    }
                    catch
                    {
                        try
                        {
                            // Current working folder
                            return Environment.CurrentDirectory;
                        }
                        catch
                        {
                            try
                            {
                                // Desktop
                                return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                            }
                            catch
                            {
                                // Also current working folder
                                return ".";
                            }
                        }
                    }
                }
            }
        }

        public static RegistryKey RegistryKey
        {
            get
            {
                try
                {
                    return
                        Registry.CurrentUser.CreateSubKey(
                            "SOFTWARE\\" + Application.CompanyName + "\\" + Application.ProductName,
                            RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                catch
                {
                    var key = Application.UserAppDataRegistry;
                    if (key == null) return key;
                    var sKeyToUse = key.ToString().Replace("HKEY_CURRENT_USER\\", "");
                    sKeyToUse = sKeyToUse.Substring(0, sKeyToUse.LastIndexOf("\\", StringComparison.Ordinal));
                    key = Registry.CurrentUser.OpenSubKey(sKeyToUse, true);
                    return key;
                }
            }
        }

        public static string Setting(string sKeyName)
        {
            string sVal = null;

            try
            {
                sVal = RegistryKey.GetValue(sKeyName, string.Empty).ToString();
            }
            catch
            {
                // ignored
            }

            return string.IsNullOrEmpty(sVal) ? string.Empty : sVal;
        }

        public static void Setting(string sKeyName, object oKeyValue)
        {
            try
            {
                if ((oKeyValue == null) || (oKeyValue.ToString() == ""))
                {
                    RegistryKey.SetValue(sKeyName, string.Empty, RegistryValueKind.String);
                    RegistryKey.DeleteValue(sKeyName);
                }
                else
                    RegistryKey.SetValue(sKeyName, oKeyValue.ToString());
            }
            catch
            {
                // ignored
            }
        }

        public static bool SettingValue(string sAppKey, bool bDefault)
        {
            try
            {
                var s = Setting(sAppKey);

                if (string.IsNullOrEmpty(s))
                    return bDefault;

                bool bRet;

                if (bool.TryParse(Setting(sAppKey.Trim()).Trim().ToLower(), out bRet))
                    return bRet;
            }
            catch
            {
                // ignored
            }

            return bDefault;
        }
    }
}