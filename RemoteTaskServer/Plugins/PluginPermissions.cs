#region

using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;

#endregion

namespace UlteriusServer.Plugins


{
    public class PluginPermissions
    {
        private static readonly byte[] SAditionalEntropy = Encoding.UTF8.GetBytes(Environment.MachineName);

        public static readonly string TrustFile = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                                                  @"\data\plugins\trusted.dat";

        public static void SaveApprovedGuids()
        {
            File.WriteAllText(TrustFile, string.Empty);
            var approvedPlugins = PluginHandler._ApprovedPlugins;
            var stringBuilder = new StringBuilder();
            foreach (var plugin in approvedPlugins)
            {
                var guid = plugin.Value;
                var name = plugin.Key;
                stringBuilder.AppendLine(name + "|" + guid);
            }
            var guidBytes = Encoding.UTF8.GetBytes(stringBuilder.ToString().Trim());
            var encryptedSecret = Protect(guidBytes);
            File.WriteAllBytes(TrustFile, encryptedSecret);
        }

        private static byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                //  only by the same current user.
                return ProtectedData.Protect(data, SAditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static bool ApprovePlugin(string guid)
        {
            try
            {
                var pendingPluginKey = PluginHandler._PendingPlugins.FirstOrDefault(x => x.Value == guid).Key;
                PluginHandler._PendingPlugins.Remove(pendingPluginKey);
                PluginHandler._ApprovedPlugins.Add(pendingPluginKey, guid);
                SaveApprovedGuids();
                //plugins approved set it up
                PluginHandler.SetupPlugin(guid);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static string GetApprovedGuids()
        {
            var originalData = File.ReadAllBytes(TrustFile);
            if (originalData.Length > 0)
            {
                var unprotected = Unprotect(originalData);
                if (unprotected != null && unprotected.Length > 0)
                {
                    var decryptedData = Encoding.UTF8.GetString(unprotected);
                    return decryptedData;
                }
            }
            return string.Empty;
        }

        private static byte[] Unprotect(byte[] data)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, SAditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Data was not decrypted. An error occurred. Deleting file");
                File.WriteAllText(TrustFile, string.Empty);
                return null;
            }
        }

        public static IPermission GetPermissionByName(string name)
        {
            switch (name)
            {
                case "System.Net.WebPermission": //Controls rights to access HTTP Internet resources.
                    return new WebPermission(PermissionState.Unrestricted);
                case "System.Drawing.Printing.PrintingPermission": //Controls access to printers
                    return new PrintingPermission(PermissionState.Unrestricted);
                case "System.Net.DnsPermission":
                    //Controls rights to access Domain Name System (DNS) servers on the network.
                    return new DnsPermission(PermissionState.Unrestricted);
                case "System.Net.Mail.SmtpPermission":
                    //Controls access to Simple Mail Transport Protocol (SMTP) servers.
                    return new SmtpPermission(PermissionState.Unrestricted);
                case "System.Net.NetworkInformation.NetworkInformationPermission":
                    //Controls access to network information and traffic statistics for the local computer. 
                    return new NetworkInformationPermission(PermissionState.Unrestricted);
                case "System.Net.SocketPermission":
                    //Controls rights to make or accept connections on a transport address.
                    return new SocketPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.DataProtectionPermission":
                    //Controls the ability to access encrypted data and memory. 
                    return new DataProtectionPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.EnvironmentPermission":
                    //Controls access to system and user environment variables.
                    return new EnvironmentPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.FileDialogPermission":
                    //Controls the ability to access files or folders through a File dialog box.
                    return new FileDialogPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.FileIOPermission":
                    //Controls the ability to access files and folders. 
                    return new FileIOPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.GacIdentityPermission":
                    //Defines the identity permission for files originating in the global assembly cache.
                    return new GacIdentityPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.KeyContainerPermission":
                    //Controls the ability to access key containers
                    return new KeyContainerPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.MediaPermission":
                    //The MediaPermission describes a set of security permissions that controls the ability for audio, 
                    //image, and video media to work in a partial-trust Windows Presentation Foundation (WPF) application.
                    return new MediaPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.PublisherIdentityPermission":
                    //Represents the identity of a software publisher.
                    return new PublisherIdentityPermission(PermissionState.Unrestricted);
                // case "System.Security.Permissions.ReflectionPermission": //Controls access to non-public types and members through the System.Reflection APIs.
                //   return new ReflectionPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.RegistryPermission":
                    //Controls the ability to access registry variables.
                    return new RegistryPermission(PermissionState.Unrestricted);
                //  case "System.Security.Permissions.SecurityPermission": //Describes a set of security permissions applied to code.
                //   return new SecurityPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.SiteIdentityPermission":
                    //Defines the identity permission for the Web site from which the code originates.
                    return new SiteIdentityPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.StorePermission":
                    //Controls access to stores containing X.509 certificates.
                    return new StorePermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.StrongNameIdentityPermission":
                    //Defines the identity permission for strong names. 
                    return new StrongNameIdentityPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.TypeDescriptorPermission":
                    //Defines partial-trust access to the TypeDescriptor class.
                    return new TypeDescriptorPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.UIPermission":
                    //Controls the permissions related to user interfaces and the Clipboard. 
                    return new UIPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.UrlIdentityPermission":
                    //Defines the identity permission for the URL from which the code originates.
                    return new UrlIdentityPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.WebBrowserPermission":
                    //The WebBrowserPermission object controls the ability to create the WebBrowser control.
                    return new WebBrowserPermission(PermissionState.Unrestricted);
                case "System.Security.Permissions.ZoneIdentityPermission":
                    //Defines the identity permission for the zone from which the code originates. 
                    return new ZoneIdentityPermission(PermissionState.Unrestricted);
            }
            return null;
        }
    }
}