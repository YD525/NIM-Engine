using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    public class ChatGptApi : I_AI_Node
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public int Type { get; set; } = 1;
        public string Name { get; set; } = "ChatGpt";
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

            ChatGptRequest Request = new ChatGptRequest();
            Request.Model = Model;
            Request.Messages = new List<ChatGptMessage>();
            Request.Messages.Add(new ChatGptMessage("user", Prompt));

            string Json = JsonConvert.SerializeObject(Request, Formatting.None);
            byte[] Body = Utf8NoBom.GetBytes(Json);

            string HttpResponseStr = Post(Endpoint, ApiKey, Body, Proxy);

            ChatGptResponse Result;
            if (TryParseResponse(HttpResponseStr, out Result))
            {
                AIResponseStr = Result.Choices[0].Message.Content.Trim();
            }

            return HttpResponseStr;
        }

        internal static bool TryParseResponse(string json, out ChatGptResponse result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.Choices != null &&
                    value.Choices.Length > 0 &&
                    value.Choices[0] != null &&
                    value.Choices[0].Message != null &&
                    !string.IsNullOrWhiteSpace(value.Choices[0].Message.Content),
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

        public class ChatGptRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("messages")]
            public List<ChatGptMessage> Messages { get; set; }
        }

        public class ChatGptMessage
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }

            public ChatGptMessage() { }

            public ChatGptMessage(string Role, string Content)
            {
                this.Role = Role;
                this.Content = Content;
            }
        }

        public class ChatGptResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("object")]
            public string Object { get; set; }

            [JsonProperty("created")]
            public long Created { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("choices")]
            public ChatGptChoice[] Choices { get; set; }

            [JsonProperty("usage")]
            public ChatGptUsage Usage { get; set; }
        }

        public class ChatGptChoice
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("message")]
            public ChatGptMessage Message { get; set; }

            [JsonProperty("finish_reason")]
            public string FinishReason { get; set; }
        }

        public class ChatGptUsage
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonProperty("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}