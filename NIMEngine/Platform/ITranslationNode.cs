using NIMEngine.Engine;
using NIMEngine.Language;
using NIMEngine.P_Delegate;
using NIMEngine.Unit;
using System.Collections.Generic;

namespace NIMEngine.Platform
{
    public interface I_AI_Node
    {
        /// <summary>
        /// Local_AI_Node = 0, AI_Node = 1,Interactive_Node = 2,Node = 3
        /// </summary>
        int Type { get; set; }
        /// <summary>
        /// Platform Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Use Model
        /// </summary>
        string Model { get; set; }

        /// <summary>
        /// Translate
        /// </summary>
        /// <param name="ApiKey">CurrentKey</param>
        /// <param name="Prompt">AI Prompt</param>
        /// <param name="AIResponseStr">AIResponse</param>
        /// <param name="Proxy">Proxy[0] = ProxyUrl,Proxy[1] = ProxyUserName,Proxy[2] = ProxyPassword</param>
        /// <returns>Return Http Response Str</returns>
        string Call(string ApiKey, string Prompt, out string AIResponseStr, string[] Proxy = null);

        /// <summary>
        /// Whether this node is built into the system (true) or provided by a plugin (false).
        /// </summary>
        bool IsSystemNode { get; set; }
    }
    public interface I_Local_AI_Node
    {
        /// <summary>
        /// Exposed lock object. External callers may poll IsLocked(SingleLock)
        /// before invoking Call to avoid blocking; Call itself also locks on it.
        /// </summary>
        object SingleLock { get; }

        /// <summary>
        /// Local_AI_Node = 0, AI_Node = 1,Interactive_Node = 2,Node = 3
        /// </summary>
        int Type { get; set; }

        /// <summary>
        /// Local AI service port, e.g. LM Studio (default 1234) or Ollama (default 11434)
        /// </summary>
        int LocalPort { get; set; }

        /// <summary>
        /// Platform Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Name of the model currently loaded in the local service.
        /// It is queried from the local service automatically, so callers do not need to set it.
        /// </summary>
        string Model { get; set; }

        /// <summary>
        /// Initializes the local AI node and retrieves the currently loaded model.
        /// </summary>
        void Init();

        string Call(string Prompt, out string AIResponseStr);

        /// <summary>
        /// Whether this node is built into the system (true) or provided by a plugin (false).
        /// </summary>
        bool IsSystemNode { get; set; }
    }
    public interface I_Node
    {
        /// <summary>
        /// Local_AI_Node = 0, AI_Node = 1,Interactive_Node = 2,Node = 3
        /// </summary>
        int Type { get; set; }

        /// <summary>
        /// Platform Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Translate text with a traditional (non-AI) translation platform.
        /// </summary>
        /// <param name="ApiKey">Current key</param>
        /// <param name="SourceText">Text to be translated</param>
        /// <param name="FromLang">Source language code</param>
        /// <param name="ToLang">Target language code</param>
        /// <param name="ResponseStr">Translated text parsed from the response</param>
        /// <param name="Proxy">Proxy[0] = ProxyUrl, Proxy[1] = ProxyUserName, Proxy[2] = ProxyPassword</param>
        /// <returns>Raw Http Response Str</returns>
        string Call(string ApiKey, string SourceText, string FromLang, string ToLang, out string ResponseStr, string[] Proxy = null);

        /// <summary>
        /// Whether this node is built into the system (true) or provided by a plugin (false).
        /// </summary>
        bool IsSystemNode { get; set; }
    }

    public interface I_Interactive_Node
    {
        /// <summary>
        /// Local_AI_Node = 0,AI_Node = 1,Interactive_Node = 2,Node = 3
        /// </summary>
        int Type { get; set; }

        /// <summary>
        /// Platform Name
        /// </summary>
        string Name { get; set; }

        string CallHuman(string Prompt, out string AIResponseStr);
    }
}
