using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.Platform;

namespace NIMEngine.Plugin.Official.NodePack
{
    public class NodePack
    {
        /// <summary>
        /// Plugin Init
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="ParamsRef">Params is a pointer.</param>
        public void Init(int Type,object[] ParamsRef)
        {
            if (Type == 10)
            {
                //Type == 10 LoadNodes

                ParamsRef = new object[3];

                ParamsRef[0] = new List<I_AI_Node>();
                ParamsRef[1] = new List<I_Local_AI_Node>();
                ParamsRef[2] = new List<I_Node>();
            }
            
        }

        
    }
}
