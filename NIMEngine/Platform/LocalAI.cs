using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    /// <summary>
    /// Generic local AI node for any OpenAI-compatible local server (LM Studio, Ollama ...).
    /// Set LocalPort before calling. The model is auto-detected during Init.
    /// </summary>
    public class LocalAI : I_Local_AI_Node
    {
        private const string Host = "http://127.0.0.1";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly Regex ThinkRegex = new Regex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.Compiled);

        public int Type { get; set; } = 0;

        /// <summary>Exposed lock object. External callers may poll IsLocked(SingleLock) before invoking Call.</summary>
        public object SingleLock { get; } = new object();

        public int LocalPort { get; set; } = 0;
        public string Name { get; set; } = "LocalAI";

        /// <summary>Auto-detected during Init, not meant to be set from outside.</summary>
        public string Model { get; set; } = "";

        /// <summary>Optional. Only sent when not empty (LM Studio "Require Authentication").</summary>
        public string ApiKey { get; set; } = "";

        public int TimeoutMs { get; set; } = 600000;
        public int ModelQueryTimeoutMs { get; set; } = 50000;

        public bool IsSystemNode { get; set; } = true;

        public void Init()
        {
            if (LocalPort <= 0)
            {
                throw new InvalidOperationException("LocalPort is not set.");
            }

            lock (SingleLock)
            {
                Model = GetCurrentModelName();

                if (string.IsNullOrEmpty(Model))
                {
                    throw new InvalidOperationException("No model found on local service, port: " + LocalPort);
                }
            }
        }

        /// <summary>
        /// Proxy is ignored, local requests never go through a proxy.
        /// </summary>
        /// <returns>Raw Http Response Str</returns>
        public string Call(string Prompt, out string AIResponseStr)
        {
            AIResponseStr = string.Empty;

            if (LocalPort <= 0)
            {
                throw new InvalidOperationException("LocalPort is not set.");
            }

            if (string.IsNullOrEmpty(Model))
            {
                throw new InvalidOperationException("Local AI has not been initialized.");
            }

            lock (SingleLock)
            {
                LocalAIItem Request = new LocalAIItem(Model);
                Request.messages.Add(new LocalAIMessage("user", Prompt));

                string Json = JsonConvert.SerializeObject(Request, Formatting.None);
                byte[] Body = Utf8NoBom.GetBytes(Json);

                string HttpResponseStr = Send("POST", Host + ":" + LocalPort + "/v1/chat/completions", Body, TimeoutMs);

                LocalAIResponse Result;
                if (TryParseResponse(HttpResponseStr, out Result))
                {
                    string Content = Result.choices[0].message.content;
                    AIResponseStr = ThinkRegex.Replace(Content, string.Empty).Trim();
                }

                return HttpResponseStr;
            }
        }

        /// <summary>Query /v1/models, first non-embedding model. Empty string when nothing is available.</summary>
        private string GetCurrentModelName()
        {
            string HttpResponseStr = Send("GET", Host + ":" + LocalPort + "/v1/models", null, ModelQueryTimeoutMs);

            LocalAIModelListResponse Result;
            if (!TryParseModelResponse(HttpResponseStr, out Result))
            {
                return string.Empty;
            }

            foreach (LocalAIModel Item in Result.data)
            {
                if (Item == null || string.IsNullOrWhiteSpace(Item.id))
                {
                    continue;
                }

                if (Item.id.IndexOf("embed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                return Item.id;
            }

            return string.Empty;
        }

        internal static bool TryParseModelResponse(string json, out LocalAIModelListResponse result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.data != null &&
                    value.data.Length > 0 &&
                    value.data[0] != null &&
                    !string.IsNullOrWhiteSpace(value.data[0].id),
                out result);
        }

        internal static bool TryParseResponse(string json, out LocalAIResponse result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.choices != null &&
                    value.choices.Length > 0 &&
                    value.choices[0] != null &&
                    value.choices[0].message != null &&
                    !string.IsNullOrWhiteSpace(value.choices[0].message.content),
                out result);
        }

        private string Send(string Method, string Url, byte[] Body, int Timeout)
        {
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(Url);
            Req.Method = Method;
            Req.UserAgent = UserAgentStr;
            Req.Accept = "application/json";
            Req.Timeout = Timeout;
            Req.ReadWriteTimeout = Timeout;
            Req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Req.Proxy = null; // never use the system proxy for local requests

            if (!string.IsNullOrEmpty(ApiKey))
            {
                Req.Headers["Authorization"] = "Bearer " + ApiKey;
            }

            if (Body != null)
            {
                Req.ContentType = "application/json; charset=utf-8";
                Req.ContentLength = Body.Length;
                using (Stream ReqStream = Req.GetRequestStream())
                {
                    ReqStream.Write(Body, 0, Body.Length);
                }
            }

            try
            {
                using (HttpWebResponse Resp = (HttpWebResponse)Req.GetResponse())
                using (Stream RespStream = Resp.GetResponseStream())
                using (StreamReader Reader = new StreamReader(RespStream, Encoding.UTF8))
                {
                    return Reader.ReadToEnd();
                }
            }
            catch (WebException ex) when (ex.Response != null)
            {
                string ErrBody;
                using (ex.Response)
                using (Stream ErrStream = ex.Response.GetResponseStream())
                using (StreamReader Reader = new StreamReader(ErrStream, Encoding.UTF8))
                {
                    ErrBody = Reader.ReadToEnd();
                }

                throw new InvalidOperationException("Local AI HTTP error (" + Url + "): " + ErrBody, ex);
            }
        }

        public class LocalAIModelListResponse
        {
            public LocalAIModel[] data { get; set; }
        }

        public class LocalAIModel
        {
            public string id { get; set; }
        }

        public class LocalAIItem
        {
            public string model { get; set; }
            public List<LocalAIMessage> messages { get; set; } = new List<LocalAIMessage>();

            public LocalAIItem() { }

            public LocalAIItem(string model)
            {
                this.model = model;
            }
        }

        public class LocalAIMessage
        {
            public string role { get; set; }
            public string content { get; set; }

            public LocalAIMessage() { }

            public LocalAIMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        public class LocalAIResponse
        {
            public string id { get; set; }
            [JsonProperty("object")]
            public string _object { get; set; }
            public long created { get; set; }
            public string model { get; set; }
            public LocalAIChoice[] choices { get; set; }
            public LocalAIUsage usage { get; set; }
            public string system_fingerprint { get; set; }
        }

        public class LocalAIUsage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
        }

        public class LocalAIChoice
        {
            public int index { get; set; }
            public object logprobs { get; set; }
            public string finish_reason { get; set; }
            public LocalAIRMessage message { get; set; }
        }

        public class LocalAIRMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}