using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.Engine;
using NIMEngine.Language;
using NIMEngine.Memory;
using NIMEngine.P_Delegate;
using NIMEngine.Plugin;
using NIMEngine.Translate;
using NIMEngine.Unit;

namespace NIMEngine.Platform
{
    public class PlatformHub
    {
        public static ConcurrentDictionary<string, I_AI_Node> AINodes { get; private set; } = new ConcurrentDictionary<string, I_AI_Node>();
        public static ConcurrentDictionary<string, I_Local_AI_Node> LocalNodes { get; private set; } = new ConcurrentDictionary<string, I_Local_AI_Node>();
        public static ConcurrentDictionary<string, I_Node> Nodes { get; private set; } = new ConcurrentDictionary<string, I_Node>();

        public static ConcurrentDictionary<string, I_Interactive_Node> HumanNodes { get; private set; } = new ConcurrentDictionary<string, I_Interactive_Node>();

        /// <summary>
        /// Global network initialization. Call once at program startup, before any request is sent.
        /// </summary>
        public static void Init()
        {
            // .NET Framework limits HttpWebRequest to 2 concurrent connections per host by default,
            // extra requests queue up and may time out. Only affects ServicePoints created after this call.
            ServicePointManager.DefaultConnectionLimit = 999;

            // Enable TLS 1.2, older .NET Framework versions do not enable it by default.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            var Results = PluginHub.Instance.Dispatch(0, "LoadNodes");

            if (Results.Executed)
            {
                foreach (var GetPluginResult in Results.Results)
                {
                    object[] Payload = GetPluginResult.Value; 
                    if (Payload == null || Payload.Length < 3) continue; // invalid payload

                    RegisterLocalAINodes(Payload[0]);
                    RegisterAINodes(Payload[1]);
                    RegisterNodes(Payload[2]);
                }
            }


            //Also, remember to synchronize the NIM_Engine.Config.
        }

        #region Register
        private static void RegisterAINodes(object Raw)
        {
            if (!(Raw is List<KeyValuePair<string, Type>> List)) return;

            foreach (var Kv in List)
            {
                if (Kv.Value == null) continue;
                try
                {
                    var Node = (I_AI_Node)Activator.CreateInstance(Kv.Value);
                    AINodes[Kv.Key] = Node;
                }
                catch (Exception Ex)
                {
                    
                }
            }
        }

        private static void RegisterLocalAINodes(object Raw)
        {
            if (!(Raw is List<KeyValuePair<string, Type>> List)) return;

            foreach (var Kv in List)
            {
                if (Kv.Value == null) continue;
                try
                {
                    var Node = (I_Local_AI_Node)Activator.CreateInstance(Kv.Value);
                    LocalNodes[Kv.Key] = Node;
                }
                catch (Exception Ex)
                {
                }
            }
        }

        private static void RegisterNodes(object Raw)
        {
            if (!(Raw is List<KeyValuePair<string, Type>> List)) return;

            foreach (var Kv in List)
            {
                if (Kv.Value == null) continue;
                try
                {
                    var Node = (I_Node)Activator.CreateInstance(Kv.Value);
                    Nodes[Kv.Key] = Node;
                }
                catch (Exception Ex)
                {
                }
            }
        }

        #endregion

        private static Dictionary<string, PlatformConfig> PlatformConfigCache = null;

        public static Dictionary<string, PlatformConfig> GetAllNode()
        {
            PlatformConfigCache = new Dictionary<string, PlatformConfig>(NIM_Engine.Config.PlatformConfigs);
            return PlatformConfigCache;
        }

        public static bool IsEnable(string Name)
        {
            if (PlatformConfigCache == null)
            {
                GetAllNode();
            }

            if (PlatformConfigCache.ContainsKey(Name))
            {
                if (PlatformConfigCache[Name].Enable)
                {
                    if (PlatformConfigCache[Name].Type == 0 || PlatformConfigCache[Name].Type == 2)
                    {
                        //Local_AI_Node and Interactive_Node do not require an API key.
                        return true;
                    }
                    else
                    {
                        PlatformApiKeys KeyData = NIMEngine.NIM_Engine.KeyData.GetData(Name);

                        if (KeyData.HaveKey())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static I_AI_Node GetAINode(string Name)
        {
            if (AINodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }
        public static bool CallAINode(string Name, string ApiKey, EngineConfigJson ConfigRef,
            List<ReplaceTag> CustomWords, UnitGroup Source, Languages FromLang, Languages ToLang, bool UseAIMemory, int AIMemoryCountLimit, string AIParam, ref string UseModel, ref AICall Call
            , out string TranslateStr)
        {
            if (AINodes.TryGetValue(Name, out var CurrentNode))
            {
                List<string> Related = new List<string>();

                bool CanTrans = false;
                string SourceStr = Source.GenContent(ref CanTrans);

                if (!CanTrans)
                {
                    TranslateStr = "<empty>";
                    return true;
                }

                if (ConfigRef.ContextEnable && UseAIMemory)
                {
                    Related = Source.QueryAIMemory(FromLang, ToLang, AIMemoryCountLimit);
                }

                if (!string.IsNullOrWhiteSpace(ConfigRef.UserCustomAIPrompt))
                {
                    AIParam = (AIParam ?? string.Empty) + "\n" + ConfigRef.UserCustomAIPrompt;
                }

                var GenAIPrompt = AIPrompt.GenerateTranslationPrompt(FromLang, ToLang, SourceStr, Related, CustomWords, AIParam);

                string SendPrompt = GenAIPrompt;

                string Response = "";

                Response = CurrentNode.Call(ApiKey, SendPrompt,
                out string AIResponseStr,
                new string[3]
                {
                    ConfigRef.ProxyUrl,
                    ConfigRef.ProxyUserName,
                    ConfigRef.ProxyPassword,
                });

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = !string.IsNullOrEmpty(AIResponseStr);

                TranslateStr = AIResponseStr;

                UseModel = CurrentNode.Model;

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }

        public static I_Local_AI_Node GetLocalAINode(string Name)
        {
            if (LocalNodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }
        public static bool CallLocalAINode(string Name,EngineConfigJson ConfigRef,
            List<ReplaceTag> CustomWords, UnitGroup Source, Languages FromLang, Languages ToLang, bool UseAIMemory, int AIMemoryCountLimit, string AIParam, ref string UseModel, ref AICall Call
            , out string TranslateStr)
        {
            if (LocalNodes.TryGetValue(Name, out var CurrentNode))
            {
                if (PlatformConfigCache == null)
                {
                    GetAllNode();
                }

                List<string> Related = new List<string>();

                bool CanTrans = false;
                string SourceStr = Source.GenContent(ref CanTrans);

                if (!CanTrans)
                {
                    TranslateStr = "<empty>";
                    return true;
                }

                if (ConfigRef.ContextEnable && UseAIMemory)
                {
                    Related = Source.QueryAIMemory(FromLang, ToLang, AIMemoryCountLimit);
                }

                if (!string.IsNullOrWhiteSpace(ConfigRef.UserCustomAIPrompt))
                {
                    AIParam = (AIParam ?? string.Empty) + "\n" + ConfigRef.UserCustomAIPrompt;
                }

                var GenAIPrompt = AIPrompt.GenerateTranslationPrompt(FromLang, ToLang, SourceStr, Related, CustomWords, AIParam);

                string SendPrompt = GenAIPrompt;

                string Response = "";

                CurrentNode.LocalPort = PlatformConfigCache[Name].LocalPort;

                Response = CurrentNode.Call(SendPrompt,
                out string AIResponseStr);

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = !string.IsNullOrEmpty(AIResponseStr);

                TranslateStr = AIResponseStr;

                UseModel = CurrentNode.Model;

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }

        public static I_Node GetNode(string Name)
        {
            if (Nodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }


        /// <summary>
        /// Call a traditional (non-AI) translation platform.
        /// Not recommended: traditional platforms cannot use AI memory, custom words context or custom prompts,
        /// and are usually worse than AI nodes on game text. Prefer <see cref="CallAINode"/> when possible.
        /// </summary>
        /// <param name="Name">Platform name registered in AllNodes</param>
        /// <param name="ApiKey">Current key</param>
        /// <param name="ConfigRef">Engine config, used for the proxy settings</param>
        /// <param name="Source">Unit to translate</param>
        /// <param name="FromLang">Source language</param>
        /// <param name="ToLang">Target language</param>
        /// <param name="Call">Call record (request / raw response / success flag)</param>
        /// <param name="TranslateStr">Translated text, "&lt;empty&gt;" when there is nothing to translate</param>
        /// <returns>true when the platform was found and executed (check Call.Success for the result), false when the platform does not exist</returns>
        public static bool CallNode(string Name, string ApiKey, EngineConfigJson ConfigRef,
            UnitGroup Source, Languages FromLang, Languages ToLang,
            ref PlatformCall Call, out string TranslateStr)
        {
            if (Nodes.TryGetValue(Name, out var CurrentNode))
            {
                // Nothing to translate
                bool CanTrans = false;

                string SourceStr = Source.GenContent(ref CanTrans);

                if (!CanTrans)
                {
                    TranslateStr = "<empty>";
                    return true;
                }

                // Node expects language code strings, convert from the Languages enum
                string FromCode = P_Language.ToLanguageCode(FromLang);
                string ToCode = P_Language.ToLanguageCode(ToLang);

                string Response = CurrentNode.Call(ApiKey, SourceStr, FromCode, ToCode,
                out string ResponseStr,
                new string[3]
                {
                    ConfigRef.ProxyUrl,
                    ConfigRef.ProxyUserName,
                    ConfigRef.ProxyPassword,
                });

                Call = new PlatformCall(Name, FromLang, ToLang, SourceStr, Response);
                Call.Success = !string.IsNullOrEmpty(ResponseStr);

                TranslateStr = ResponseStr;

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }


        public static I_Interactive_Node GetHumanNode(string Name)
        {
            if (HumanNodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }

        public static bool CallHumanNode(string Name, EngineConfigJson ConfigRef,
            List<ReplaceTag> CustomWords, UnitGroup Source, Languages FromLang, Languages ToLang, bool UseAIMemory, int AIMemoryCountLimit, string AIParam, ref AICall Call
            , out string TranslateStr)
        {
            if (HumanNodes.TryGetValue(Name, out var CurrentNode))
            {
                List<string> Related = new List<string>();

                bool CanTrans = false;
                string SourceStr = Source.GenContent(ref CanTrans);

                if (!CanTrans)
                {
                    TranslateStr = "<empty>";
                    return true;
                }

                if (ConfigRef.ContextEnable && UseAIMemory)
                {
                    Related = Source.QueryAIMemory(FromLang, ToLang, AIMemoryCountLimit);
                }

                if (!string.IsNullOrWhiteSpace(ConfigRef.UserCustomAIPrompt))
                {
                    AIParam = (AIParam ?? string.Empty) + "\n" + ConfigRef.UserCustomAIPrompt;
                }

                var GenAIPrompt = AIPrompt.GenerateTranslationPrompt(FromLang, ToLang, SourceStr, Related, CustomWords, AIParam);

                string SendPrompt = GenAIPrompt;

                string Response = "";

                Response = CurrentNode.CallHuman(SendPrompt,
                out string AIResponseStr);

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = !string.IsNullOrEmpty(AIResponseStr);

                TranslateStr = AIResponseStr;

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }
    }
}