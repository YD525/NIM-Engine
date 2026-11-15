using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    public class ClaudeApi : I_AI_Node
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public int Type { get; set; } = 1;

        public string Name { get; set; } = "Claude";
        public string Model { get; set; } = "";
        public int MaxTokens { get; set; } = 8192;
        public int TimeoutMs { get; set; } = 120000;

        public bool IsSystemNode { get; set; } = false;

        /// <summary>
        /// Proxy[0] = ProxyUrl, Proxy[1] = ProxyUserName, Proxy[2] = ProxyPassword
        /// </summary>
        /// <returns>Raw Http Response Str</returns>
        public string Call(string ApiKey, string Prompt, out string AIResponseStr, string[] Proxy = null)
        {
            AIResponseStr = string.Empty;

            ClaudeRequest Request = new ClaudeRequest();
            Request.Model = Model;
            Request.MaxTokens = MaxTokens;
            Request.Messages = new List<ClaudeMessage>();
            Request.Messages.Add(new ClaudeMessage("user", Prompt));

            string Json = JsonConvert.SerializeObject(Request, Formatting.None);
            byte[] Body = Utf8NoBom.GetBytes(Json);

            string HttpResponseStr = Post(Endpoint, ApiKey, Body, Proxy);

            ClaudeResponse Result;
            if (TryParseResponse(HttpResponseStr, out Result))
            {
                AIResponseStr = GetText(Result).Trim();
            }

            return HttpResponseStr;
        }

        internal static bool TryParseResponse(string json, out ClaudeResponse result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.Content != null &&
                    value.Content.Length > 0 &&
                    !string.IsNullOrWhiteSpace(GetText(value)),
                out result);
        }

        /// <summary>Concatenate all "text" blocks, skipping thinking / tool blocks.</summary>
        private static string GetText(ClaudeResponse Response)
        {
            StringBuilder Sb = new StringBuilder();
            foreach (ClaudeContentBlock Block in Response.Content)
            {
                if (Block != null && Block.Type == "text" && Block.Text != null)
                {
                    Sb.Append(Block.Text);
                }
            }
            return Sb.ToString();
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
            Req.Headers.Add("x-api-key", ApiKey);
            Req.Headers.Add("anthropic-version", ApiVersion);

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

        public class ClaudeRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonProperty("messages")]
            public List<ClaudeMessage> Messages { get; set; }
        }

        public class ClaudeMessage
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }

            public ClaudeMessage() { }

            public ClaudeMessage(string Role, string Content)
            {
                this.Role = Role;
                this.Content = Content;
            }
        }

        public class ClaudeResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("content")]
            public ClaudeContentBlock[] Content { get; set; }

            [JsonProperty("stop_reason")]
            public string StopReason { get; set; }

            [JsonProperty("usage")]
            public ClaudeUsage Usage { get; set; }
        }

        public class ClaudeContentBlock
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }
        }

        public class ClaudeUsage
        {
            [JsonProperty("input_tokens")]
            public int InputTokens { get; set; }

            [JsonProperty("output_tokens")]
            public int OutputTokens { get; set; }
        }
    }
}