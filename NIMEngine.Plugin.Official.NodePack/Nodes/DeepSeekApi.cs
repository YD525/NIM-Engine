using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    public class DeepSeekApi : I_AI_Node
    {
        private const string Endpoint = "https://api.deepseek.com/chat/completions";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public int Type { get; set; } = 1;

        public string Name { get; set; } = "DeepSeek";
        public string Model { get; set; } = "";
        public int TimeoutMs { get; set; } = 120000;

        public bool IsSystemNode { get; set; } = false;

        /// <summary>
        /// Proxy[0] = ProxyUrl, Proxy[1] = ProxyUserName, Proxy[2] = ProxyPassword
        /// </summary>
        /// <returns>Raw Http Response Str</returns>
        public string Call(string ApiKey, string Prompt, out string AIResponseStr, string[] Proxy = null)
        {
            AIResponseStr = string.Empty;

            DeepSeekItem Request = new DeepSeekItem();
            Request.model = Model;
            Request.stream = false;
            Request.messages = new List<DeepSeekMessage>();
            Request.messages.Add(new DeepSeekMessage("user", Prompt));

            string Json = JsonConvert.SerializeObject(Request, Formatting.None);
            byte[] Body = Utf8NoBom.GetBytes(Json);

            string HttpResponseStr = Post(Endpoint, ApiKey, Body, Proxy);

            DeepSeekRootobject Result;
            if (TryParseResponse(HttpResponseStr, out Result))
            {
                AIResponseStr = Result.choices[0].message.content.Trim();
            }

            return HttpResponseStr;
        }

        internal static bool TryParseResponse(string json, out DeepSeekRootobject result)
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

        private string Post(string Url, string ApiKey, byte[] Body, string[] Proxy)
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2

            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(Url);
            Req.Method = "POST";
            Req.UserAgent = UserAgentStr;
            Req.Accept = "application/json";
            Req.ContentType = "application/json; charset=utf-8";
            Req.ContentLength = Body.Length;
            Req.Timeout = TimeoutMs;
            Req.ReadWriteTimeout = TimeoutMs;
            Req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Req.Headers.Add("Authorization", "Bearer " + ApiKey);

            if (Proxy != null && Proxy.Length > 0 && !string.IsNullOrWhiteSpace(Proxy[0]))
            {
                WebProxy NProxy = new WebProxy(Proxy[0]);
                if (Proxy.Length > 2 && !string.IsNullOrEmpty(Proxy[1]))
                {
                    NProxy.Credentials = new NetworkCredential(Proxy[1], Proxy[2]);
                }
                Req.Proxy = NProxy;
            }

            using (Stream ReqStream = Req.GetRequestStream())
            {
                ReqStream.Write(Body, 0, Body.Length);
            }

            using (HttpWebResponse Resp = (HttpWebResponse)Req.GetResponse())
            using (Stream RespStream = Resp.GetResponseStream())
            using (StreamReader Reader = new StreamReader(RespStream, Encoding.UTF8))
            {
                return Reader.ReadToEnd();
            }
        }

        public class DeepSeekItem
        {
            public string model { get; set; }
            public List<DeepSeekMessage> messages { get; set; }
            public bool stream { get; set; }
        }

        public class DeepSeekMessage
        {
            public string role { get; set; }
            public string content { get; set; }

            public DeepSeekMessage() { }

            public DeepSeekMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        public class DeepSeekRootobject
        {
            public string id { get; set; }
            [JsonProperty("object")]
            public string _object { get; set; }
            public long created { get; set; }
            public string model { get; set; }
            public DeepSeekChoice[] choices { get; set; }
            public DeepSeekUsage usage { get; set; }
            public string system_fingerprint { get; set; }
        }

        public class DeepSeekUsage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
            public DeepSeekPrompt_Tokens_Details prompt_tokens_details { get; set; }
            public int prompt_cache_hit_tokens { get; set; }
            public int prompt_cache_miss_tokens { get; set; }
        }

        public class DeepSeekPrompt_Tokens_Details
        {
            public int cached_tokens { get; set; }
        }

        public class DeepSeekChoice
        {
            public int index { get; set; }
            public DeepSeekRMessage message { get; set; }
            public object logprobs { get; set; }
            public string finish_reason { get; set; }
        }

        public class DeepSeekRMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
}