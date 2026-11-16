using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NIMEngine.Plugin
{
    // Copyright (c) 2026 YD525
    public class PluginHub
    {
        public static PluginHub Instance = new PluginHub();
        public sealed class PluginEntry
        {
            public I_Plugin Instance { get; internal set; }
            public string FilePath { get; internal set; }
            public string Name => Instance?.Name;
            public string Author => Instance?.Author;
            public string Description => Instance?.Description;
            public bool Enabled => Instance != null && Instance.Enabled;
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

                        I_Plugin Instance;
                        try { Instance = (I_Plugin)Activator.CreateInstance(T); }
                        catch (Exception) { continue; }

                        PluginEntry Entry = new PluginEntry
                        {
                            Instance = Instance,
                            FilePath = Dll,
                            IsLoaded = true
                        };

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
                object[] Args = new object[] { MethodName, ParamsRef };
                try
                {
                    if (Type == 0)
                        Entry.Instance.Init(MethodName, ref Args);
                    else if (Type == 1)
                        Entry.Instance.HandleEvent(MethodName, ref Args);
                    else
                        continue;

                    Executed = true;

                    if (Args[1] is object[] Payload)
                    {
                        Results[Entry.FilePath] = Payload;
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