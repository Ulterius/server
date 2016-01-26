using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UlteriusPlugins;


namespace UlteriusServer.Plugins
{
    public static class PluginLoader<T>
    {
        private static string path = "./data/plugins/";
        public static List<string> BrokenPlugins = new List<string>(); 

        public static ICollection<IPlugin> LoadPlugins()
        {
            if (!Directory.Exists(path)) return null;
            var installedPlugins = Directory.GetFiles(path, "*.dll").ToList();

            var assemblies = new List<Assembly>(installedPlugins.Count);
            assemblies.AddRange(installedPlugins.Select(AssemblyName.GetAssemblyName).Select(Assembly.Load));

            var pluginType = typeof(T);
           var pluginTypes = (from assembly in assemblies where assembly != null from type in GetTypes(assembly) where !type.IsInterface && !type.IsAbstract where type.GetInterface(pluginType.FullName) != null select type).ToList();
            var plugins = new List<IPlugin>(pluginTypes.Count);
            plugins.AddRange(pluginTypes.Select(type => (IPlugin) Activator.CreateInstance(type)));

            return plugins;
        }

        private static Type[] GetTypes(Assembly assembly)
        {
            
            try
            {
                return assembly.GetTypes();
            }
            catch (Exception)
            {
                //swallow it baby
                BrokenPlugins.Add(assembly.FullName);
                return Type.EmptyTypes;
            }
        }
    }
}