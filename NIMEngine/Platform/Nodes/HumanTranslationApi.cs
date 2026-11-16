using System;
using System.Collections.Generic;
using NIM_Plugin.Node;
using NIMEngine.Engine;
using NIMEngine.Language;
using NIMEngine.Memory;
using NIMEngine.P_Delegate;
using NIMEngine.Translate;
using NIMEngine.Unit;

namespace NIMEngine.Platform
{
    public class HumanTranslationApi : I_Interactive_Node
    {
        public int Type { get; set; } = 2;
        public string Name { get; set; } = "HumanTranslation";

        public EngineConfigJson ConfigRef { get; set; } = null;
        public AITranslationMemory AIMemoryRef { get; set; } = null;

        public bool IsSystemNode { get; set; } = true;

        public static AwaitHumanTranslationHandler WaitHumanInput = null;
        public delegate string AwaitHumanTranslationHandler(string SendStr);

        public void Init(AITranslationMemory AIMemory, EngineConfigJson Config)
        {
            this.AIMemoryRef = AIMemory;
            this.ConfigRef = Config;
        }

        public string CallHuman(string Prompt, out string AIResponseStr)
        {
            AIResponseStr = string.Empty;

            if (WaitHumanInput == null)
            {
                throw new Exception("Null Func Ptr!");
            }

            string Recv = HumanTranslationApi.WaitHumanInput.Invoke(Prompt);

            AIResponseStr = Recv;

            return Recv;
        }
    }
}