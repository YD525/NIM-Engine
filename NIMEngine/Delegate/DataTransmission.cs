using System.Collections.Generic;
using NIMEngine.Engine;
using NIMEngine.Events;
using NIMEngine.Language;
using NIMEngine.Translate;

namespace NIMEngine.P_Delegate
{
    public enum CallType
    {
        Null = 0, CacheCall = 1, PreTranslateCall = 2, PlatformCall = 3, AICall = 5
    }

    public class DataTransmission
    {
        public static void Recv(CallType Type, object Any)
        {
            Recv((int)Type, Any);
        }
        public static void Recv(int Type, object Any)
        {
            if (EngineEvents.SetDataCall != null)
            {
                EngineEvents.SetDataCall(Type, Any);
            }
        }
    }

    public class PreTranslateCall
    {
        public string Key = "";
        public string Name = "PreTranslate";
        public string SendString = "";
        public string ReceiveString = "";
        public List<ReplaceTag> ReplaceTags = new List<ReplaceTag>();

        public bool FromAI = false;

        public PreTranslateCall()
        {
        }

        public void Output()
        {
            DataTransmission.Recv(CallType.PreTranslateCall, this);
        }
    }

    public class CacheCall
    {
        public string SendString = "";
        public string ReceiveString = "";
        public string Log = "";

        public CacheCall()
        {

        }

        public void Output()
        {
            DataTransmission.Recv(CallType.CacheCall, this);
        }
    }

    public class PlatformCall
    {
        public string Name = "";
        public Languages From = Languages.Null;
        public Languages To = Languages.Null;
        public string SendString = "";
        public string ReceiveString = "";
        public bool Success = false;

        public PlatformCall() { }
        public PlatformCall(string Name, Languages From, Languages To, string Send, string Receive)
        {
            this.Name = Name;
            this.From = From;
            this.To = To;
            this.SendString = Send;
            this.ReceiveString = Receive;
        }

        public void Output()
        {
            DataTransmission.Recv(CallType.PlatformCall, this);
        }
    }
    public class AICall
    {
        public string Name = "";
        public string SendString = "";
        public string ReceiveString = "";
        public bool Success = false;

        public AICall() { }
        public AICall(string Name, string Send, string Receive)
        {
            this.Name = Name;
            SendString = Send;
            ReceiveString = Receive;
        }

        public void Output()
        {
            DataTransmission.Recv(CallType.AICall, this);
        }
    }
}
