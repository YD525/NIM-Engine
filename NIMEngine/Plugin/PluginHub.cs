using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NIMEngine.Platform;

namespace NIMEngine.Plugin
{
    // Copyright (c) 2026 YD525
    public class PluginHub
    {
        /// <summary>
        /// The whitelist only records the signatures of official mods.
        /// </summary>
        public static List<string> Verified_SHA256_List = new List<string>() { };

        public static PluginHub Instance = new PluginHub();
        public sealed class PluginEntry
        {
            public I_Plugin Instance { get; internal set; }
            public string FilePath { get; internal set; }
            public Type PluginType { get; internal set; }
            public string Name { get; internal set; }
            public string Author { get; internal set; }
            public string Description { get; internal set; }
            public string Version { get; internal set; }

            public string SHA256_Sign = "";

            public bool Enabled = false;
            public bool IsLoaded { get; internal set; }
            public Exception LastError { get; internal set; }
        }

        private readonly List<PluginEntry> Plugins = new List<PluginEntry>();
        private object _Lock = new object();

        public void Init()
        {
            foreach (string Dll in Directory.GetFiles(NIM_Engine.GetFullPath(NIM_Engine.PluginsPath), "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    Assembly Asm = Assembly.LoadFrom(Dll);

                    Type[] Types;
                    try { Types = Asm.GetTypes(); }
                    catch (ReflectionTypeLoadException Ex) { Types = Ex.Types; }

                    foreach (Type T in Types)
                    {
                        if (T == null || T.IsAbstract || T.IsInterface) continue;
                        if (!typeof(I_Plugin).IsAssignableFrom(T)) continue;

                        PluginNameAttribute NameAttr = T.GetCustomAttribute<PluginNameAttribute>();
                        PluginAuthorAttribute AuthorAttr = T.GetCustomAttribute<PluginAuthorAttribute>();
                        PluginDescriptionAttribute DescriptionAttr = T.GetCustomAttribute<PluginDescriptionAttribute>();
                        PluginVersionAttribute VersionAttr = T.GetCustomAttribute<PluginVersionAttribute>();
                        PluginEnabledAttribute EnabledAttr = T.GetCustomAttribute<PluginEnabledAttribute>();

                        if (NameAttr == null || AuthorAttr == null || DescriptionAttr == null || VersionAttr == null || EnabledAttr == null)
                        {
                            // [Violating plugin] Simply skip any plugins that don't behave as expected.
                            continue;
                        }                  

                        PluginEntry Entry = new PluginEntry
                        {
                            Instance = null, // Do not create the instance early, otherwise Enabled is meaningless.
                            FilePath = Dll,
                            PluginType = T,
                            SHA256_Sign = SHA256_Validator.ComputeFileSHA256(Dll),
                            Enabled = false,
                            IsLoaded = true
                        };

                        if (EnabledAttr.Enabled && !Verified_SHA256_List.Contains(Entry.SHA256_Sign))
                        {
                            // [Violating plugin] Simply skip any plugins that don't behave as expected.
                            continue;
                        }

                        Entry.Name = NameAttr?.Name;
                        Entry.Author = AuthorAttr?.Author;
                        Entry.Description = DescriptionAttr?.Description;
                        Entry.Version = VersionAttr?.Version.ToString("F2");

                        if (EnabledAttr.Enabled && Verified_SHA256_List.Contains(Entry.SHA256_Sign))
                        {
                            Entry.Enabled = true;
                        }

                        lock (_Lock)
                        {
                            Plugins.Add(Entry);
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public (bool Executed, Dictionary<string, object[]> Results) Dispatch(int Type, string MethodName, object[] ParamsRef = null)
        {
            bool Executed = false;
            Dictionary<string, object[]> Results = new Dictionary<string, object[]>();

            List<PluginEntry> Snapshot;
            lock (_Lock) { Snapshot = Plugins.Where(P => P.IsLoaded && P.Enabled).ToList(); }

            foreach (PluginEntry Entry in Snapshot)
            {
                if (Entry.Instance == null)
                {
                    Entry.Instance = (I_Plugin)Activator.CreateInstance(Entry.PluginType);
                }

                object[] Args = ParamsRef;
                try
                {
                    if (Type == 0)
                        Entry.Instance.Init(MethodName, ref Args);
                    else if (Type == 1)
                        Entry.Instance.HandleEvent(MethodName, ref Args);
                    else
                        continue;

                    Executed = true;

                    if (Args != null)
                    {
                        Results[Entry.FilePath] = Args;
                    }
                }
                catch (Exception Ex)
                {
                    Entry.LastError = Ex;
                    Executed = true;
                }
            }

            return (Executed, Results);
        }

        public List<PluginEntry> GetPlugins()
        {
            lock (_Lock)
            {
                return Plugins.ToList();
            }
        }
    }
}