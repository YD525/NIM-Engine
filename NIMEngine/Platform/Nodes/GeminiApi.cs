using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    public class GeminiApi : I_AI_Node
    {
        private const string EndpointFormat = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public int Type { get; set; } = 1;
        public string Name { get; set; } = "Gemini";
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

            GeminiItem Request = new GeminiItem();
            Request.contents.Add(new GeminiContent());
            Request.contents[0].parts.Add(new GeminiPart());
            Request.contents[0].parts[0].text = Prompt;

            string Json = JsonConvert.SerializeObject(Request, Formatting.None);
            byte[] Body = Utf8NoBom.GetBytes(Json);

            string Url = string.Format(EndpointFormat, Model);

            string HttpResponseStr = Post(Url, ApiKey, Body, Proxy);

            GeminiRootobject Result;
            if (TryParseResponse(HttpResponseStr, out Result))
            {
                AIResponseStr = Result.candidates[0].content.parts[0].text.Trim();
            }

            return HttpResponseStr;
        }

        internal static bool TryParseResponse(string json, out GeminiRootobject result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.candidates != null &&
                    value.candidates.Length > 0 &&
                    value.candidates[0] != null &&
                    value.candidates[0].content != null &&
                    value.candidates[0].content.parts != null &&
                    value.candidates[0].content.parts.Count > 0 &&
                    value.candidates[0].content.parts[0] != null &&
                    !string.IsNullOrWhiteSpace(value.candidates[0].content.parts[0].text),
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
            Req.Headers.Add("x-goog-api-key", ApiKey);

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

        public class GeminiItem
        {
            public List<GeminiContent> contents { get; set; } = new List<GeminiContent>();
        }

        public class GeminiContent
        {
            public List<GeminiPart> parts { get; set; } = new List<GeminiPart>();
        }

        public class GeminiPart
        {
            public string text { get; set; }
        }

        public class GeminiRootobject
        {
            public GeminiCandidate[] candidates { get; set; }
            public GeminiUsagemetadata usageMetadata { get; set; }
            public string modelVersion { get; set; }
        }

        public class GeminiUsagemetadata
        {
            public int promptTokenCount { get; set; }
            public int candidatesTokenCount { get; set; }
            public int totalTokenCount { get; set; }
            public GeminiPrompttokensdetail[] promptTokensDetails { get; set; }
            public GeminiCandidatestokensdetail[] candidatesTokensDetails { get; set; }
        }

        public class GeminiPrompttokensdetail
        {
            public string modality { get; set; }
            public int tokenCount { get; set; }
        }

        public class GeminiCandidatestokensdetail
        {
            public string modality { get; set; }
            public int tokenCount { get; set; }
        }

        public class GeminiCandidate
        {
            public GeminiContent content { get; set; }
            public string finishReason { get; set; }
            public float avgLogprobs { get; set; }
        }
    }
}