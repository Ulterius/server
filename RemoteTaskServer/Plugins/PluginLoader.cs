#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using Magnum.FileSystem;
using UlteriusPluginBase;
using UlteriusServer.Utilities;
using Directory = System.IO.Directory;

#endregion

namespace UlteriusServer.Plugins
{
    public class PluginLoader
    {
        public static readonly string PluginPath = Path.Combine(AppEnvironment.DataPath, "Plugins");

        public static ConcurrentDictionary<string, PluginHost> Plugins = new ConcurrentDictionary<string, PluginHost>();
        public static ConcurrentDictionary<string, string> BrokenPlugins = new ConcurrentDictionary<string, string>();


        public static T GetAttribute<T>(ICustomAttributeProvider assembly, bool inherit = false)
            where T : Attribute
        {
            return assembly
                .GetCustomAttributes(typeof(T), inherit)
                .OfType<T>()
                .FirstOrDefault();
        }

        private static StrongName GetStrongName(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            AssemblyName assemblyName = assembly.GetName();

            // Get the public key blob. 
            byte[] publicKey = assemblyName.GetPublicKey();
            if (publicKey == null || publicKey.Length == 0)
                throw new InvalidOperationException("Assembly is not strongly named");

            StrongNamePublicKeyBlob keyBlob = new StrongNamePublicKeyBlob(publicKey);

            // Return the strong name. 
            return new StrongName(keyBlob, assemblyName.Name, assemblyName.Version);
        }

        public static void LoadPlugins()
        {
            Console.WriteLine(PluginPath);
            if (!Directory.Exists(PluginPath))
            {
                Directory.CreateDirectory(PluginPath);
                return;
            }
            var installedPlugins = Directory.GetFiles(PluginPath, "*.dll").ToList();
            foreach (var installedPlugin in installedPlugins)
            {
                try
                {

                    var pluginAssembly = Assembly.LoadFrom(installedPlugin);
                    foreach (Type pluginType in pluginAssembly.GetTypes())
                    {
                        if (pluginType.IsPublic && !pluginType.IsAbstract)
                        {
                            Type typeInterface = pluginType.GetInterface("UlteriusPluginBase.IUlteriusPlugin", true);
                            if (typeInterface != null)
                            {
                                var runnable = (IUlteriusPlugin)Activator.CreateInstance(pluginAssembly.GetType(pluginType.ToString()));
                                var guid = GetAttribute<GuidAttribute>(pluginAssembly).Value;
                                var title = GetAttribute<AssemblyTitleAttribute>(pluginAssembly).Title;
                                var company = GetAttribute<AssemblyCompanyAttribute>(pluginAssembly).Company;
                                var copyright = GetAttribute<AssemblyCopyrightAttribute>(pluginAssembly).Copyright;
                                var trademark = GetAttribute<AssemblyTrademarkAttribute>(pluginAssembly).Trademark;
                                var fileVersion = GetAttribute<AssemblyFileVersionAttribute>(pluginAssembly).Version;
                                var description = GetAttribute<AssemblyDescriptionAttribute>(pluginAssembly).Description;
                                var pluginHost = new PluginHost
                                {
                                    FileVersion = fileVersion,
                                    Description = description,
                                    Title = title,
                                    Company = company,
                                    Copyright = copyright,
                                    Trademark = trademark,
                                    Guid = guid,
                                    Instance = runnable,
                                    Website = runnable.Website,
                                    JavaScript = runnable.Javascript,
                                    Icon = runnable.Icon,
                                    RequiresSetup = runnable.RequiresSetup
                                };
                                Plugins.TryAdd(guid, pluginHost);
                            }
                        }
                    }

                  
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var sb = new StringBuilder();
                    foreach (var exSub in ex.LoaderExceptions)
                    {
                        sb.AppendLine(exSub.Message);
                        var exFileNotFound = exSub as FileNotFoundException;
                        if (!string.IsNullOrEmpty(exFileNotFound?.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                        sb.AppendLine();
                    }
                    var errorMessage = sb.ToString();
                    Console.WriteLine(errorMessage);
                }
            }
        }
    }
}