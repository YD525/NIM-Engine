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
            string PluginDir = NIM_Engine.GetFullPath(NIM_Engine.PluginsPath);

            // Directory missing: skip everything, and never prune (would wipe the user's switches).
            if (!Directory.Exists(PluginDir)) return;

            HashSet<string> Seen = new HashSet<string>();
            bool ScanOk = true;

            string[] Dlls;
            try
            {
                Dlls = Directory.GetFiles(PluginDir, "*.dll", SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                return;
            }

            foreach (string Dll in Dlls)
            {
                string Sign;
                try
                {
                    Sign = SHA256_Validator.ComputeFileSHA256(Dll);
                }
                catch (Exception)
                {
                    // Cannot identify this DLL (file locked, etc.), so the scan is incomplete: do not prune.
                    ScanOk = false;
                    continue;
                }

                // A DLL that exists on disk counts as "seen" even if it fails to load below,
                // so a temporarily broken DLL does not lose the user's switch.
                Seen.Add(Sign);

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

                        bool Verified = Verified_SHA256_List.Contains(Sign);

                        if (EnabledAttr.Enabled && !Verified)
                        {
                            // [Violating plugin] Claims to be official but is not in the whitelist.
                            continue;
                        }

                        PluginEntry Entry = new PluginEntry
                        {
                            Instance = null, // Do not create the instance early, otherwise Enabled is meaningless.
                            FilePath = Dll,
                            PluginType = T,
                            SHA256_Sign = Sign,
                            Name = NameAttr.Name,
                            Author = AuthorAttr.Author,
                            Description = DescriptionAttr.Description,
                            Version = VersionAttr.Version.ToString("F1", CultureInfo.InvariantCulture),
                            Enabled = EnabledAttr.Enabled && Verified
                        };

                        if (!NIM_Engine.Config.Plugins.ContainsKey(Sign))
                        {
                            NIM_Engine.Config.Plugins.Add(Sign, Entry.Enabled);
                        }

                        lock (_Lock)
                        {
                            Plugins.Add(Entry);
                        }
                    }
                }
                catch (Exception)
                {
                    // Load failed: skip this DLL. Its hash is already in Seen, so the user's switch is kept.
                }
            }

            if (ScanOk)
            {
                PruneInvalidKeys(Seen);
            }
        }

        /// <summary>
        /// Remove config entries whose DLL no longer exists in the plugin directory.
        /// </summary>
        private static void PruneInvalidKeys(HashSet<string> Seen)
        {
            List<string> Stale = NIM_Engine.Config.Plugins.Keys
                .Where(K => !Seen.Contains(K))
                .ToList(); // ToList first: cannot remove while enumerating

            foreach (string Key in Stale)
            {
                NIM_Engine.Config.Plugins.Remove(Key);
            }
        }

        public (bool Executed, Dictionary<string, object[]> Results) Dispatch(int Type, string MethodName, object[] ParamsRef = null)
        {
            bool Executed = false;
            Dictionary<string, object[]> Results = new Dictionary<string, object[]>();

            if (Type < 0 || Type > 1)
            {
                return (false, Results);
            }

            List<PluginEntry> Snapshot;
            lock (_Lock) { Snapshot = Plugins.ToList(); }

            foreach (PluginEntry Entry in Snapshot)
            {
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