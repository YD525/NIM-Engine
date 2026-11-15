using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.Platform;

namespace NIMEngine.Plugin.Official.NodePack
{
    public class NodePack: I_Plugin
    {
        public string Name { get; set; } = "Node Pack";
        public string Author { get; set; } = "NIM Engine";
        public string Description { get; set; } = "Node extension pack that registers multiple translation platform nodes so the engine can use them.";

        /// <summary>
        /// Called by the engine when the plugin is loaded.
        /// </summary>
        /// <param name="MethodName">Name of the feature currently being loaded.</param>
        /// <param name="ParamsRef">Params is a pointer,Used to pass or return data.</param>
        public void Init(string MethodName, ref object[] ParamsRef)
        {
            if (MethodName == "LoadNodes")
            {
                ParamsRef = new object[3];

                ParamsRef[0] = new List<KeyValuePair<string, Type>>()
                {
                    // Local_AI_Node
                    new KeyValuePair<string,Type>("LocalAI",typeof(LocalAI))
                };

                ParamsRef[1] = new List<KeyValuePair<string, Type>>()
                {
                    // I_AI_Node
                    new KeyValuePair<string,Type>("Claude",typeof(ClaudeApi)),
                    new KeyValuePair<string,Type>("ChatGpt",typeof(ChatGptApi)),
                    new KeyValuePair<string,Type>("Gemini",typeof(GeminiApi)),
                    new KeyValuePair<string,Type>("NanoGpt",typeof(NanoGptApi)),
                    new KeyValuePair<string,Type>("DeepSeek",typeof(DeepSeekApi))
                }; 

                ParamsRef[2] = new List<KeyValuePair<string, Type>>()
                {
                    // Node
                    new KeyValuePair<string,Type>("DeepL",typeof(DeepLApi))
                };
            }
        }

        /// <summary>
        /// Called by the engine when an event occurs, so the plugin can handle it.
        /// </summary>
        /// <param name="MethodName">Name of the event method raised by the engine.</param>
        /// <param name="ParamsRef">Params is a pointer,Used to pass or return data.</param>
        public void HandleEvent(string MethodName,ref object[] ParamsRef)
        { 
            
        }
    }
}
