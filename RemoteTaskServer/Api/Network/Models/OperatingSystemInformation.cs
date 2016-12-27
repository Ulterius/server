#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;
using NetFwTypeLib;

#endregion

namespace UlteriusServer.Api.Network.Models

{
    public static class OperatingSystemInformation
    {

        public static string Name { get; set; }
        public static string Version { get; set; }
        public static uint MaxProcessCount { get; set; }
        public static ulong MaxProcessRam { get; set; }
        public static string Architecture { get; set; }
        public static string SerialNumber { get; set; }
        public static string Build { get; set; }
        public static string Json { get; set; }

        private static bool UacOn()
        {
            var uac_key =
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System");
            return uac_key?.GetValue("EnableLUA") != null && uac_key.GetValue("EnableLUA").ToString() == "1";
        }

        private static bool FirewallOn()
        {
            var netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            var mgr = (INetFwMgr) Activator.CreateInstance(netFwMgrType);
            return mgr.LocalPolicy.CurrentProfile.FirewallEnabled;
        }

        private static List<string> InstalledNetFrameworks()
        {
            var path = @"SOFTWARE\Microsoft\NET Framework Setup\NDP";
            var displayFramworkName = new List<string>();

            var installedVersions = Registry.LocalMachine.OpenSubKey(path);
            if (installedVersions == null) return displayFramworkName;
            var versionNames = installedVersions.GetSubKeyNames();
            for (var i = 1; i <= versionNames.Length - 1; i++)
            {
                var openSubKey = installedVersions.OpenSubKey(versionNames[i]);
                if (openSubKey == null) continue;
                var tempName = $"Microsoft .NET Framework {versionNames[i]}  SP{openSubKey.GetValue("SP")}";
                displayFramworkName.Add(tempName);
            }
            displayFramworkName.Add(Get45Or451FromRegistry());
            return displayFramworkName;
        }

        private static string Get45Or451FromRegistry()
        {
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey == null) return null;
                var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                if (true)
                {
                    return $"Microsoft .NET Framework {CheckFor45DotVersion(releaseKey)}";
                }
            }
        }
        private static string CheckFor45DotVersion(int releaseKey)
        {
            if (releaseKey >= 393273)
            {
                return "4.6 RC or later";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2 or later";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1 or later";
            }
            return (releaseKey >= 378389) ? "4.5 or later" : "No 4.5 or later version detected";
            // This line should never execute. A non-null release key should mean 
            // that 4.5 or later is installed. 
        }

  

        private static Dictionary<string, string> GetAntiVirusInfo(string product = "AntiVirusProduct")
        {
            // Looks for the wanted class in the WMI namespace
            // (applicable for AntiVirusProduct, AntiSpywareProduct, FirewallProduct)
            // Using Windows XP systems you have to replace SecurityCenter2 with SecurityCenter.
            var objSearcher =
                new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM " + product);

            // NameValueCollection where the result should be saved
            var outputCollection = new Dictionary<string, string>();

            foreach (var o in objSearcher.Get())
            {
                var queryObj = (ManagementObject) o;
                foreach (var propertyData in queryObj.Properties)
                {
                    // Add found properties to the collection
                    outputCollection.Add(propertyData.Name, propertyData.Value.ToString());
                }
            }

            return outputCollection;
        }

        public static Dictionary<string, string> GetServices()
        {
            var services = ServiceController.GetServices();
          
           return services.ToDictionary(s => s.ServiceName, s => s.Status.ToString());
        
        }

        public static Dictionary<string, string> GetUserEnvironmentVariables()
        {
            return System.Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User).Cast<DictionaryEntry>().ToDictionary(e => e.Key.ToString(), e => e.Value.ToString());
        }

        public static Dictionary<string, string> GetMachineEnvironmentVariables()
        {
            return System.Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine).Cast<DictionaryEntry>().ToDictionary(e => e.Key.ToString(), e => e.Value.ToString());
        }

     
        public static object ToObject()
        {
            var data = new
            {
                name = Name,
                version = Version,
                maxProcessCount = MaxProcessCount,
                maxProcessRam = MaxProcessRam,
                architecture = Architecture,
                serialNumber = SerialNumber,
                build = Build
            };
            return data;
        }
    }
}