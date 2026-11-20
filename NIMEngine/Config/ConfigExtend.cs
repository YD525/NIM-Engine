using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using NIM_Plugin;
using NIMEngine.Common;
using NIMEngine.Platform;
using NIMEngine.Translate;

namespace NIMEngine
{
    public class PlatformConfig
    {
        /// <summary>
        /// This is the unique primary key; it cannot have any conflicts.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Local_AI_Node = 0, AI_Node = 1,Interactive_Node = 2,Node = 3
        /// </summary>
        public int Type = 0;

        public List<string> ApiKeys { get; set; } = new List<string>();
     
        public bool Enable = false;
        public string Model { get; set; } = "";

        public int LocalPort = 0;

        public PlatformConfig()
        {
        }
    }
    public class EngineConfigJson
    {
        #region Init

        public int Init = 0;

        #endregion

        #region PluginConfig

        /// <summary>
        /// Loaded plugins:UniqueKey = plugin file path, value = enabled switch (true = enabled, false = disabled).
        /// </summary>
        public Dictionary<string, bool> Plugins { get; set; } = new Dictionary<string, bool>();

        #endregion

        #region RequestConfig

        /// <summary>
        /// Configured http proxy or local proxy for network requests.
        /// </summary>
        public string ProxyUrl { get; set; } = "";

        public string ProxyUserName { get; set; } = "";

        public string ProxyPassword { get; set; } = "";

        #endregion

        #region DataBase

        /// <summary>
        /// Default page size for pagination.  
        /// Represents how many items are shown per page by default.
        /// </summary>
        public int DefPageSize { get; set; } = 50;

        #endregion


        public bool PreTranslateEnable { get; set; } = true;


        #region Platform Cls


        #endregion

        #region ApiKey Set

        public Dictionary<string, PlatformConfig> PlatformConfigs { get; set; } = null;

        #endregion

        #region EngineSetting

        /// <summary>
        /// The ratio of the maximum thread count at which throttling is triggered. 
        /// Range is 0 to 1, default is 0.7 meaning throttling starts when over 70% usage.
        /// </summary>
        public double ThrottleRatio { get; set; } = 0.7;

        /// <summary>
        /// The sleep time in milliseconds for the main thread during throttling. Default is 200ms.
        /// </summary>
        public int ThrottleDelayMs { get; set; } = 200;

        /// <summary>
        /// Specifies the maximum number of threads to use for processing.
        /// This value determines the upper limit of concurrent threads the system can use.
        /// </summary>

        public int MaxThreadCount { get; set; } = 2;

        /// <summary>
        /// Indicates whether to automatically set the maximum number of threads.
        /// If true, the system will determine and apply a suitable thread limit based on hardware or configuration.
        /// </summary>
        public bool AutoSetThreadLimit { get; set; } = false;

        /// <summary>
        /// Indicates whether to enable context-based generation.
        /// If true, the process will consider contextual information;  
        /// if false, it will only handle the current string without any context.
        /// </summary>
        public bool ContextEnable { get; set; } = true;

        /// <summary>
        /// Bucket-assignment similarity threshold (lower bound of the Jaccard coefficient).
        /// During BuildBuckets, each pending Unit is compared against every existing
        /// Head bucket using the Jaccard similarity |A∩B| / |A∪B|
        /// (A = the Head's token signature, B = the Unit's token signature).
        /// The Head bucket with the highest similarity is selected; the Unit is added
        /// to that bucket only when the highest value is ≥ this threshold.
        /// Otherwise the Unit forms its own independent bucket (with no Head).
        /// Higher values → stricter assignment, more independent buckets;
        /// lower values → looser assignment, fewer and larger buckets.
        /// Note: this threshold applies only to "Unit vs Head" bucket assignment.
        /// It is NOT used when choosing Heads in the ChooseHeads stage,
        /// nor for deduplicating Head against Head.
        /// </summary>
        public double BucketAssignmentSimilarityThreshold_WordBoundary = 0.25;
        public double BucketAssignmentSimilarityThreshold_NoSpace = 0.17;

        /// <summary>
        /// Specifies the maximum number of context characters to include during generation.
        /// For example, if set to 200, the total character count of all context lines will not exceed 200.
        /// </summary>
        public int ContextLimit { get; set; } = 200;

        /// <summary>
        /// Determine the size of each bucket.
        /// </summary>
        public int BucketLengthLimit { get; set; } = 4500;

        /// <summary>
        /// Always send the complete conversation context to the AI, including already translated lines, to ensure translation consistency and preserve the original dialogue flow.
        /// </summary>
        public bool PreserveConversationContext { get; set; } = false;


        /// <summary>
        /// Enable duplicate removal in confirmed dialogue relationships. Similarity-based groups are always deduplicated, while this option also removes duplicates from fully related dialogue contexts. This can reduce AI token usage but may affect dialogue context completeness.
        /// </summary>
        public bool ForceContextDeduplication { get; set; } = false;

        /// <summary>
        /// Whether to enforce absolute purity of "Link Buckets" (Type == 1 — buckets holding
        /// units that have a real, established link relationship in the ESP file).
        ///
        /// Enabled (true): once built, Link Buckets can never have additional content appended
        /// or merged into them (orphan-unit backfill, similarity-bucket merging, etc. all skip
        /// Link Buckets). Every unit inside a Link Bucket is guaranteed to come from the same
        /// genuine ESP link chain — nothing unrelated gets mixed in. This matters because these
        /// buckets are sent together as shared context; letting unrelated content leak in can
        /// confuse the AI about which entries are actually related, leading to wrong context
        /// inference or inconsistent translations.
        ///
        /// Disabled (false, default): Link Buckets may still receive extra content appended to
        /// them whenever there's remaining capacity, which improves packing efficiency and
        /// reduces the total bucket count, at the cost of allowing unrelated entries into an
        /// otherwise link-only bucket.
        /// </summary>
        public bool StrictLinkBucketPurity { get; set; } = false;

        /// <summary>
        /// The system decides to completely disconnect a node after it fails a few times, and then restore the connection when the next batch translation begins.
        /// </summary>
        public int FailThreshold { get; set; } = 10;

        /// <summary>
        /// Allows retrieval of the entire database using only the source text.
        /// </summary>
        public bool EnableGlobalSearch { get; set; } = false;


        /// <summary>
        /// Protective symbols such as <(.*?)> are created and applied to the TranslationPreprocessor to prevent certain characters from being mistranslated.
        /// </summary>
        public List<string> ProtectedPatterns = new List<string>();

        /// <summary>
        /// Configure the maximum number of rejections,Used to prevent infinite loops.
        /// </summary>
        public int MaxTranslationAttempts = 3;

        /// <summary>
        /// Waiting time for retrying.
        /// </summary>
        public int ReTryWaitTime = 1000;//ms

        #endregion

        /// <summary>
        /// User-defined custom prompt sent to the AI model.
        /// This prompt can be used to guide the AI's behavior or translation style.
        /// </summary>
        public string UserCustomAIPrompt { get; set; } = "";

      
        public string GetPlatformKeysStr(PlatformConfig Config)
        {
            lock (NIM_Engine.QueryPlatformDataLock)
            {
                string KeysStr = "";

                for (int i = 0; i < Config.ApiKeys.Count; i++)
                {
                    KeysStr += Config.ApiKeys[i] + ";\n";
                }

                return KeysStr;
            }
        }

        public List<string> KeysStrToArray(string KeysStr)
        {
            KeysStr = KeysStr.Replace("\r\n", "");
            KeysStr = KeysStr.Replace("\n", "");

            List<string> Keys = new List<string>();
            foreach (var GetLine in KeysStr.Split(';'))
            {
                if (GetLine.Trim().Length > 0)
                {
                    Keys.Add(GetLine.Trim());
                }
            }
            return Keys;
        }
    }

    public abstract class ConfigExtend
    {
        public static EngineConfigJson Config = new EngineConfigJson();

        private static readonly byte[] XorKey = Encoding.UTF8.GetBytes("NIMEngine");

        public static void SetDefaultModel()
        {
            if (Config.PlatformConfigs == null)
            {
                Config.PlatformConfigs = new Dictionary<string, PlatformConfig>();
            }
        }
        private static byte[] XOREncrypt(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ XorKey[i % XorKey.Length]);
            }
            return result;
        }

        private static byte[] XORDecrypt(byte[] data)
        {
            return XOREncrypt(data);
        }

        //Use Xor to easily encrypt and store user API keys to ensure security
        public static void SaveConfig()
        {
            NIM_Engine.Config.Plugins = NIMPlugin.Plugins;

            string GetJson = JsonConvert.SerializeObject(Config);
            var EncryptedBytes = XOREncrypt(Encoding.UTF8.GetBytes(GetJson));
            File.WriteAllBytes(NIM_Engine.GetFullPath("EngineConfig.data"), EncryptedBytes);
        }

        public static bool CheckAvailableNodes()
        {
            int EnableCount = 0;
            for (int i = 0; i < NIM_Engine.Config.PlatformConfigs.Count; i++)
            { 
                var GetKey = NIM_Engine.Config.PlatformConfigs.ElementAt(i).Key;

                if (NIM_Engine.Config.PlatformConfigs[GetKey].ApiKeys.Count > 0)
                {
                    if (NIM_Engine.Config.PlatformConfigs[GetKey].Enable)
                    {
                        EnableCount++;
                    }
                }
                else
                if (NIM_Engine.Config.PlatformConfigs[GetKey].Name.Equals("LocalAI") && NIM_Engine.Config.PlatformConfigs[GetKey].Enable)
                {
                    EnableCount++;
                }
                else
                if (NIM_Engine.Config.PlatformConfigs[GetKey].Name.Equals("HumanTranslation") && NIM_Engine.Config.PlatformConfigs[GetKey].Enable)
                {
                    EnableCount++;
                }
            }
            if (EnableCount > 0)
            {
                return true;
            }
            return false;
        }
        public static void LoadConfig()
        {
            NextCall:
            string SetFullPath = NIM_Engine.GetFullPath("EngineConfig.data");
            if (!File.Exists(SetFullPath))
            {
                SetDefaultModel();
                SaveConfig();
                return;
            }
            else
            {
                try
                {
                    if (new FileInfo(SetFullPath).Length > JsonPayload.MaximumDocumentBytes)
                        throw new InvalidDataException("The engine configuration exceeds the JSON size limit.");
                    var DecryptedBytes = XORDecrypt(File.ReadAllBytes(SetFullPath));
                    EngineConfigJson LoadedConfig;
                    if (!JsonPayload.TryDeserialize(
                        Encoding.UTF8.GetString(DecryptedBytes),
                        value => value != null,
                        out LoadedConfig))
                    {
                        throw new InvalidDataException("The engine configuration is invalid.");
                    }
                    Config = LoadedConfig;

                    SetDefaultModel();
                }
                catch
                {
                    SaveConfig();
                    goto NextCall;
                }
            }
        }
    }
}
