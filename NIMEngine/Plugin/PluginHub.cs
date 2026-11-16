using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static readonly IReadOnlyList<string> Verified_SHA256_List = new List<string>() { }.AsReadOnly();

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
                            Enabled = false
                        };

                        if (EnabledAttr.Enabled && !Verified_SHA256_List.Contains(Entry.SHA256_Sign))
                        {
                            // [Violating plugin] Simply skip any plugins that don't behave as expected.
                            continue;
                        }

                        Entry.Name = NameAttr?.Name;
                        Entry.Author = AuthorAttr?.Author;
                        Entry.Description = DescriptionAttr?.Description;
                        Entry.Version = VersionAttr.Version.ToString("F1", CultureInfo.InvariantCulture);

                        if (EnabledAttr.Enabled && Verified_SHA256_List.Contains(Entry.SHA256_Sign))
                        {
                            Entry.Enabled = true;
                        }

                        if (!NIM_Engine.Config.Plugins.ContainsKey(Entry.SHA256_Sign))
                        {
                            NIM_Engine.Config.Plugins.Add(Entry.SHA256_Sign, Entry.Enabled);
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
            lock (_Lock) { Snapshot = Plugins.ToList(); }

            foreach (PluginEntry Entry in Snapshot)
            {
                if (Type < 0 || Type > 1)
                {
                    return (false, Results);
                }

                if (string.IsNullOrEmpty(Entry.SHA256_Sign) || !NIM_Engine.Config.Plugins.ContainsKey(Entry.SHA256_Sign))
                {
                    continue;
                }

                if (!NIM_Engine.Config.Plugins[Entry.SHA256_Sign])
                {
                    continue;
                }

                object[] Args = ParamsRef;

                try
                {
                    if (Entry.Instance == null)
                    {
                        Entry.Instance = (I_Plugin)Activator.CreateInstance(Entry.PluginType);
                    }

                    if (Type == 0)
                    {
                        Entry.Instance.Init(MethodName, ref Args);
                    }
                    else if (Type == 1)
                    {
                        Entry.Instance.HandleEvent(MethodName, ref Args);
                    }

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