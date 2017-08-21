using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LiveRoku {
    public class PluginLoader {
        public static List<T> LoadAll<T> (string folder, IAssemblyCaches caches = null) {
            var plugins = new List<T> ();
            var paths = Directory.GetFiles (folder, "*.dll");
            foreach (var path in paths) {
                var plugin = Load<T> (path, caches);
                if (plugin == null) continue;
                plugins.Add (plugin);
            }
            return plugins;
        }

        public static IEnumerable<Type> LoadImpl<T> (string path, IAssemblyCaches caches = null) {
            if (File.Exists (path) && path.EndsWith (".dll", true, null)) {
                try {
                    var name = AssemblyName.GetAssemblyName (path);
                    Assembly assembly = null;
                    if (caches == null || !caches.tryGet (name.FullName, out assembly)) {
                        System.Diagnostics.Debug.WriteLine ("Loading Assembly " + path);
                        assembly = Assembly.LoadFrom (path);
                    }
                    var types = assembly?.GetTypes ();
                    if (types == null || types.Length == 0)
                        return null;
                    var pluginType = typeof (T);
                    return from type in types where pluginType.IsAssignableFrom (type) select type;
                } catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine (e.Message);
                    System.Diagnostics.Debug.WriteLine (e.StackTrace);
                }
            }
            return null;
        }

        public static T Load<T> (string path, IAssemblyCaches caches = null) {
            if (File.Exists (path) && path.EndsWith (".dll", true, null)) {
                try {
                    var name = AssemblyName.GetAssemblyName (path);
                    Assembly assembly = null;
                    if (caches == null || !caches.tryGet (name.FullName, out assembly)) {
                        System.Diagnostics.Debug.WriteLine ("Loading Assembly " + path);
                        assembly = Assembly.LoadFrom (path);
                    }
                    var types = assembly?.GetTypes ();
                    if (types == null || types.Length == 0)
                        return default (T);
                    var pluginType = typeof (T);
                    foreach (var current in types) {
                        if (pluginType.IsAssignableFrom ((Type) current)) {
                            return (T) Activator.CreateInstance (current);
                        }
                    }
                } catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine (e.Message);
                    System.Diagnostics.Debug.WriteLine (e.StackTrace);
                }
            }
            return default (T);
        }
    }
}