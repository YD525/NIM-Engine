using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NIM_Plugin;
using NIM_Plugin.Core;
using NIM_Plugin.Node;
using NIMEngine.Engine;
using NIMEngine.Language;
using NIMEngine.Memory;
using NIMEngine.P_Delegate;
using NIMEngine.Translate;
using NIMEngine.Unit;

namespace NIMEngine.Platform
{
    public class PlatformHub
    {
        public static ConcurrentDictionary<string, I_AI_Node> CloudAINodes { get; private set; } = new ConcurrentDictionary<string, I_AI_Node>();
        public static ConcurrentDictionary<string, I_Local_AI_Node> LocalAINodes { get; private set; } = new ConcurrentDictionary<string, I_Local_AI_Node>();
        public static ConcurrentDictionary<string, I_Node> CloudNodes { get; private set; } = new ConcurrentDictionary<string, I_Node>();

        public static ConcurrentDictionary<string, I_Interactive_Node> HumanNodes { get; private set; } = new ConcurrentDictionary<string, I_Interactive_Node>();

        public static void Init((bool Executed, Dictionary<string, object[]> Results) Results)
        {
            // .NET Framework limits HttpWebRequest to 2 concurrent connections per host by default,
            // extra requests queue up and may time out. Only affects ServicePoints created after this call.
            ServicePointManager.DefaultConnectionLimit = 999;

            // Enable TLS 1.2, older .NET Framework versions do not enable it by default.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            CloudAINodes.Clear();
            LocalAINodes.Clear();
            CloudNodes.Clear();

            HumanNodes.Clear();

            HumanNodes["HumanTranslation"] = new HumanTranslationApi();

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
            List<object> AllNodes = new List<object>();

            AllNodes.AddRange(LocalAINodes.Values.ToList());
            AllNodes.AddRange(CloudAINodes.Values.ToList());
            AllNodes.AddRange(CloudNodes.Values.ToList());

            AllNodes.Add(new HumanTranslationApi());

            ActiveNodes.Add("HumanTranslation");

            foreach (var GetNode in AllNodes)
            {
                if (GetNode is I_Interactive_Node)
                {
                    var Node = (I_Interactive_Node)GetNode;

                    if (!NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        NIM_Engine.Config.PlatformConfigs.Add(Node.Name,
                            new PlatformConfig()
                            {
                                Name = Node.Name,
                                Type = Node.Type,

                                Enable = false,
                            });
                    }
                    else
                    {
                        if (NIM_Engine.Config.PlatformConfigs[Node.Name] == null)
                        {
                            NIM_Engine.Config.PlatformConfigs[Node.Name] =
                            new PlatformConfig()
                            {
                               Name = Node.Name,
                               Type = Node.Type,
                               Enable = false,
                            };
                        }
                    }
                }
                else
                if (GetNode is I_AI_Node)
                {
                    var Node = (I_AI_Node)GetNode;
                    if (!NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        NIM_Engine.Config.PlatformConfigs.Add(Node.Name,
                               new PlatformConfig()
                               {
                                   Name = Node.Name,
                                   Type = Node.Type,
                                   ApiKeys = new List<string>(),
                                   Enable = false,
                                   Model = Node.Model
                               });
                    }
                    else
                    {
                        if (NIM_Engine.Config.PlatformConfigs[Node.Name] == null)
                        {
                            NIM_Engine.Config.PlatformConfigs[Node.Name] =
                            new PlatformConfig()
                            {
                                Name = Node.Name,
                                Type = Node.Type,
                                ApiKeys = new List<string>(),
                                Enable = false,
                                Model = Node.Model
                            };
                        }
                        else
                        {
                            NIM_Engine.Config.PlatformConfigs[Node.Name].Model = Node.Model;
                        }
                    }
                }
                else
                if (GetNode is I_Local_AI_Node)
                {
                    var Node = (I_Local_AI_Node)GetNode;
                    if (!NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        NIM_Engine.Config.PlatformConfigs.Add(Node.Name,
                              new PlatformConfig()
                              {
                                  Name = Node.Name,
                                  Type = Node.Type,
                                  Enable = false,
                                  Model = Node.Model,
                                  LocalPort = Node.LocalPort,
                              });
                    }
                    else
                    {
                        if (NIM_Engine.Config.PlatformConfigs[Node.Name] == null)
                        {
                            NIM_Engine.Config.PlatformConfigs[Node.Name] =
                            new PlatformConfig()
                            {
                                Name = Node.Name,
                                Type = Node.Type,
                                Enable = false,
                                Model = Node.Model,
                                LocalPort = Node.LocalPort,
                            };
                        }
                    }
                }
                else
                if (GetNode is I_Node)
                {
                    var Node = (I_Node)GetNode;
                    if (!NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        NIM_Engine.Config.PlatformConfigs.Add(Node.Name,
                             new PlatformConfig()
                             {
                                 Name = Node.Name,
                                 Type = Node.Type,

                                 ApiKeys = new List<string>(),
                                 Enable = false,
                             });
                    }
                    else
                    {
                        if (NIM_Engine.Config.PlatformConfigs[Node.Name] == null)
                        {
                            NIM_Engine.Config.PlatformConfigs[Node.Name] =
                            new PlatformConfig()
                            {
                                Name = Node.Name,
                                Type = Node.Type,

                                ApiKeys = new List<string>(),
                                Enable = false,
                            };
                        }

                    }
                }
            }
        }

        public static HashSet<string> ActiveNodes = new HashSet<string>();
        public static bool IsNodeActive(string Key)
        {
            if (ActiveNodes.Contains(Key))
            {
                return true;
            }

            return false;
        }

        public static List<string> QueryModels(string Key)
        {
            if (CloudAINodes.ContainsKey(Key))
            {
                return CloudAINodes[Key].AvailableModels.ToList();
            }

            return new List<string>();
        }

        #region Register
        private static void RegisterAINodes(object Raw)
        {
            if (!(Raw is List<Type> List)) return;

            foreach (var Kv in List)
            {
                try
                {
                    var Node = (I_AI_Node)Activator.CreateInstance(Kv);

                    string GetDefModel = "";

                    if (NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        GetDefModel = NIM_Engine.Config.PlatformConfigs[Node.Name].Model;
                    }

                    CloudAINodes[Node.Name] = Node;

                    if (!string.IsNullOrEmpty(GetDefModel) && Node.AvailableModels.Contains(GetDefModel))
                    {
                        CloudAINodes[Node.Name].Model = GetDefModel;
                    }
                    else
                    if (Node.AvailableModels.Count() > 0)
                    {
                        CloudAINodes[Node.Name].Model = Node.AvailableModels[0];
                    }

                    ActiveNodes.Add(Node.Name);
                }
                catch (Exception)
                {
                }
            }
        }
        private static void RegisterLocalAINodes(object Raw)
        {
            if (!(Raw is List<Type> List)) return;

            foreach (var Kv in List)
            {
                try
                {
                    var Node = (I_Local_AI_Node)Activator.CreateInstance(Kv);

                    int GetDefPort = 0;

                    if (NIM_Engine.Config.PlatformConfigs.ContainsKey(Node.Name))
                    {
                        GetDefPort = NIM_Engine.Config.PlatformConfigs[Node.Name].LocalPort;
                    }

                    LocalAINodes[Node.Name] = Node;

                    if (GetDefPort != 0)
                    {
                        LocalAINodes[Node.Name].LocalPort = GetDefPort;
                    }

                    ActiveNodes.Add(Node.Name);
                }
                catch (Exception)
                {
                }
            }
        }
        private static void RegisterNodes(object Raw)
        {
            if (!(Raw is List<Type> List)) return;

            foreach (var Kv in List)
            {
                try
                {
                    var Node = (I_Node)Activator.CreateInstance(Kv);
                    CloudNodes[Node.Name] = Node;

                    ActiveNodes.Add(Node.Name);
                }
                catch (Exception)
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
            if (CloudAINodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }
        public static bool CallAINode(string Name, string ApiKey, EngineConfigJson ConfigRef,
            List<ReplaceTag> CustomWords, UnitGroup Source, Languages FromLang, Languages ToLang, bool UseAIMemory, int AIMemoryCountLimit, string AIParam, ref string UseModel, ref AICall Call
            , out string TranslateStr)
        {
            if (CloudAINodes.TryGetValue(Name, out var CurrentNode))
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
                string AIResponseStr = string.Empty;
                bool Success;

                try
                {
                    Response = CurrentNode.Call(ApiKey, SendPrompt,
                    out AIResponseStr,
                    new string[3]
                    {
                        ConfigRef.ProxyUrl,
                        ConfigRef.ProxyUserName,
                        ConfigRef.ProxyPassword,
                    });

                    Success = !string.IsNullOrEmpty(AIResponseStr);
                }
                catch (Exception Ex)
                {
                    Response = "Exception: " + Ex.Message;
                    AIResponseStr = string.Empty;
                    Success = false;
                }

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = Success;

                TranslateStr = AIResponseStr;

                try { UseModel = CurrentNode.Model; } catch (Exception) { }

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }

        public static I_Local_AI_Node GetLocalAINode(string Name)
        {
            if (LocalAINodes.TryGetValue(Name, out var CurrentNode))
            {
                return CurrentNode;
            }

            return null;
        }
        public static bool CallLocalAINode(string Name, EngineConfigJson ConfigRef,
            List<ReplaceTag> CustomWords, UnitGroup Source, Languages FromLang, Languages ToLang, bool UseAIMemory, int AIMemoryCountLimit, string AIParam, ref string UseModel, ref AICall Call
            , out string TranslateStr)
        {
            if (LocalAINodes.TryGetValue(Name, out var CurrentNode))
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
                string AIResponseStr = string.Empty;
                bool Success;

                try
                {
                    CurrentNode.LocalPort = PlatformConfigCache[Name].LocalPort;

                    Response = CurrentNode.Call(SendPrompt,
                    out AIResponseStr);

                    Success = !string.IsNullOrEmpty(AIResponseStr);

                    try 
                    {
                        if (NIM_Engine.Config.PlatformConfigs.ContainsKey(Name))
                        {
                            NIM_Engine.Config.PlatformConfigs[Name].Model = CurrentNode.Model;
                        }
                    } 
                    catch(Exception)
                    { 
                    }
                }
                catch (Exception Ex)
                {
                    Response = "Exception: " + Ex.Message;
                    AIResponseStr = string.Empty;
                    Success = false;
                }

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = Success;

                TranslateStr = AIResponseStr;

                try { UseModel = CurrentNode.Model; } catch (Exception) { }

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }

        public static I_Node GetNode(string Name)
        {
            if (CloudNodes.TryGetValue(Name, out var CurrentNode))
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
        /// <param name="TranslateStr">Translated text, "<empty>" when there is nothing to translate</param>
        /// <returns>true when the platform was found and executed (check Call.Success for the result), false when the platform does not exist</returns>
        public static bool CallNode(string Name, string ApiKey, EngineConfigJson ConfigRef,
            UnitGroup Source, Languages FromLang, Languages ToLang,
            ref PlatformCall Call, out string TranslateStr)
        {
            if (CloudNodes.TryGetValue(Name, out var CurrentNode))
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

                string Response = "";
                string ResponseStr = string.Empty;
                bool Success;

                try
                {
                    Response = CurrentNode.Call(ApiKey, SourceStr, FromCode, ToCode,
                    out ResponseStr,
                    new string[3]
                    {
                        ConfigRef.ProxyUrl,
                        ConfigRef.ProxyUserName,
                        ConfigRef.ProxyPassword,
                    });

                    Success = !string.IsNullOrEmpty(ResponseStr);
                }
                catch (Exception Ex)
                {
                    Response = "Exception: " + Ex.Message;
                    ResponseStr = string.Empty;
                    Success = false;
                }

                Call = new PlatformCall(Name, FromLang, ToLang, SourceStr, Response);
                Call.Success = Success;

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
                string AIResponseStr = string.Empty;
                bool Success;

                try
                {
                    Response = CurrentNode.CallHuman(SendPrompt,
                    out AIResponseStr);

                    Success = !string.IsNullOrEmpty(AIResponseStr);
                }
                catch (Exception Ex)
                {
                    Response = "Exception: " + Ex.Message;
                    AIResponseStr = string.Empty;
                    Success = false;
                }

                Call = new AICall(Name, SendPrompt, Response);
                Call.Success = Success;

                TranslateStr = AIResponseStr;

                return true;
            }

            TranslateStr = string.Empty;
            return false;
        }
    }
}