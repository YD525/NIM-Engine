using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.Platform;

namespace NIMEngine.Plugin.Official.NodePack
{
    [PluginName("Node Pack")] //Enter your plugin name here.
    [PluginAuthor("NIM Engine")] //Remember to use a different author name—definitely don’t use the one I’ve written here.
    [PluginDescription("Node extension pack that registers multiple translation platform nodes so the engine can use them.")] //Write your description here
    [PluginVersion(1.0f)]// Two-part format: Major.Revision
    [PluginEnabled(true)] // Third-party plugin: always <c>false</c>.
    public class NodePack: I_Plugin
    {
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
                    //Need to implement the I_Local_AI_Node interface
                    new KeyValuePair<string,Type>("LocalAI",typeof(LocalAI))
                };

                ParamsRef[1] = new List<KeyValuePair<string, Type>>()
                {
                    //Need to implement the I_AI_Node interface
                    new KeyValuePair<string,Type>("Claude",typeof(ClaudeApi)),
                    new KeyValuePair<string,Type>("ChatGpt",typeof(ChatGptApi)),
                    new KeyValuePair<string,Type>("Gemini",typeof(GeminiApi)),
                    new KeyValuePair<string,Type>("NanoGpt",typeof(NanoGptApi)),
                    new KeyValuePair<string,Type>("DeepSeek",typeof(DeepSeekApi))
                }; 

                ParamsRef[2] = new List<KeyValuePair<string, Type>>()
                {
                    //Need to implement I_Node interface
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
