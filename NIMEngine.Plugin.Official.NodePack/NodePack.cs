using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.Platform;

namespace NIMEngine.Plugin.Official.NodePack
{
    public class NodePack: IPlugin
    {
        public string Name { get; set; } = "Node Pack";
        public string Author { get; set; } = "NIM Engine";
        public string Description { get; set; } = "Node extension pack that registers multiple translation platform nodes so the engine can use them.";

        /// <summary>
        /// Called by the engine when the plugin is loaded.
        /// </summary>
        /// <param name="MethodName">Name of the feature currently being loaded.</param>
        /// <param name="ParamsRef">Params is a pointer,Used to pass or return data.</param>
        public void Init(string MethodName, object[] ParamsRef)
        {
            if (MethodName == "LoadNodes")
            {
                //Type == 10 LoadNodes

                ParamsRef = new object[3];

                ParamsRef[0] = new List<I_AI_Node>();
                ParamsRef[1] = new List<I_Local_AI_Node>();
                ParamsRef[2] = new List<I_Node>();
            }
            
        }

        /// <summary>
        /// Called by the engine when an event occurs, so the plugin can handle it.
        /// </summary>
        /// <param name="MethodName">Name of the event method raised by the engine.</param>
        /// <param name="ParamsRef">Params is a pointer,Used to pass or return data.</param>
        public void HandleEvent(string MethodName,object[] ParamsRef)
        { 
        
        }
    }
}
