using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NIM_Plugin;
using NIM_Plugin.Core;
using NIMEngine.Additional;
using NIMEngine.ADO;
using NIMEngine.Engine;
using NIMEngine.Engine.ADO;
using NIMEngine.Http;
using NIMEngine.Language;
using NIMEngine.Memory;
using NIMEngine.Platform;
using NIMEngine.Translate;

namespace NIMEngine
{
    public class NIM_Engine : ConfigExtend
    {
        //The engine allows the creation of multiple instances of the Translator, each capable of translating its own content, but sharing a single AIMemory. This improves context utilization.
        public static AITranslationMemory AIMemory = new AITranslationMemory();
        public static string Version = "3.3.9.5";

        public static object QueryPlatformDataLock = new object();

        /// <summary>
        /// Instance of the local SQLite database helper.
        /// Represents the pointer/reference to the current local database.
        /// </summary>
        public static P_SQLite LocalDB = new P_SQLite();
        public static KeyManage KeyData = new KeyManage();

        public static void SyncTrdCount()
        {
            if (Config.AutoSetThreadLimit)
            {
                Config.MaxThreadCount = NIM_Engine.AutoCalcThreadLimit();
            }
        }

        /// <summary>
        /// Automatically limit the number of concurrent threads
        /// </summary>
        /// <returns></returns>
        public static int AutoCalcThreadLimit()
        {
            int AutoThread = 0;

            bool LocalAIEnable = false;

            for (int i = 0; i < NIM_Engine.Config.PlatformConfigs.Count; i++)
            { 
                string GetKey = NIM_Engine.Config.PlatformConfigs.ElementAt(i).Key;
                var GetConfig = NIM_Engine.Config.PlatformConfigs[GetKey];

                if (GetConfig.ApiKeys.Count > 0 && GetConfig.Enable && GetConfig.Name != "LocalAI")
                {
                    AutoThread++;
                }
                
                if (GetConfig.Name == "LocalAI" && GetConfig.Enable)
                {
                    LocalAIEnable = true;
                }
            }

            if (AutoThread <= 1 && LocalAIEnable)
            {
                AutoThread++;
            }

            return AutoThread;
        }
        public static string GetVersion()
        {
            return NIM_Engine.Version;
        }

        public static string PluginVersion = "";
        private static string CurrentPath = "";
        public static void Init(string StartupPath,Action<int>StepAction = null)
        {
            CurrentPath = StartupPath;

            StepAction?.Invoke(0);
            NIM_Engine.LoadConfig();

            PluginVersion = NIMPlugin.Version;
            NIMPlugin.Init(StartupPath,NIM_Engine.Config.Plugins);

            string GetFilePath = GetFullPath(@"\Engine.db");

            StepAction?.Invoke(1);
            PluginHub.Instance.Init();//Must be called first, before the first load.

            if (!File.Exists(GetFilePath))
            {
                StepAction?.Invoke(2);
                P_SQLite.CreateDataBase(GetFilePath);
            }

            LocalDB.OpenSQL(GetFilePath);

            StepAction?.Invoke(3);
            AdvancedDictionary.Init();

            StepAction?.Invoke(5);
            CloudDBCache.Init();
            LocalDBCache.Init();

            HistoryDBCache.Init();
            FontColorFinder.Init();

            StepAction?.Invoke(6);
            ChineseVariantMap.Init();//<- 111

            StepAction?.Invoke(7);
            UniqueKeyHelper.Init();

            StepAction?.Invoke(8);
            ProxyCenter.UsingProxy();

            StepAction?.Invoke(9);
            WordAutoComplete.DatabaseDirectory = GetFullPath(NIMPlugin.PluginsPath);
            WordAutoComplete.Init();

            StepAction?.Invoke(10);
            PlatformHub.Init();
            ReSetKeyData();
        }

        public static void Vacuum()
        {
            LocalDB.ExecuteNonQuery("vacuum");
        }

        public static string GetFullPath(string Path)
        {
            string GetShellPath = CurrentPath;

            if (Path.StartsWith(@"\"))
            {
                Path = Path.Substring(1);
            }

            return GetShellPath + Path;
        }
        public static void ReSetKeyData()
        {
            KeyData = new KeyManage();
            KeyData.Init();
        }

        public static bool DisablePlugin(string Key)
        {
            return PluginHub.Instance.DisablePlugin(Key);
        }
        public static bool EnablePlugin(string Key)
        {
            return PluginHub.Instance.EnablePlugin(Key);
        }
        public static List<string[]> GetAllPlugins()
        {
            return PluginHub.Instance.GetPlugins();
        }
    }
}
