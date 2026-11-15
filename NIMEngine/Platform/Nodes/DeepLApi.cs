using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NIMEngine.Common;

namespace NIMEngine.Platform
{
    public class DeepLApi : I_Node
    {
        private const string FreeEndpoint = "https://api-free.deepl.com/v2/translate";
        private const string PaidEndpoint = "https://api.deepl.com/v2/translate";
        private const string UserAgentStr = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public int Type { get; set; } = 3;
        public string Name { get; set; } = "DeepL";
        public int TimeoutMs { get; set; } = 120000;

        public bool IsSystemNode { get; set; } = false;

        /// <summary>
        /// Proxy[0] = ProxyUrl, Proxy[1] = ProxyUserName, Proxy[2] = ProxyPassword
        /// </summary>
        /// <returns>Raw Http Response Str</returns>
        public string Call(string ApiKey, string SourceText, string FromLang, string ToLang, out string ResponseStr, string[] Proxy = null)
        {
            ResponseStr = string.Empty;

            DeepLItem Request = new DeepLItem();
            Request.target_lang = (ToLang ?? string.Empty).ToUpper();
            Request.text = new List<string>() { SourceText };

            string Json = JsonConvert.SerializeObject(Request, Formatting.None);
            byte[] Body = Utf8NoBom.GetBytes(Json);

            string HttpResponseStr = Post(IsFreeKey(ApiKey) ? FreeEndpoint : PaidEndpoint, ApiKey, Body, Proxy);

            DeepLResult Result;
            if (TryParseResponse(HttpResponseStr, out Result))
            {
                ResponseStr = Result.translations[0].text;
            }

            return HttpResponseStr;
        }

        /// <summary>DeepL free keys always end with ":fx" and must use the api-free host.</summary>
        private static bool IsFreeKey(string ApiKey)
        {
            return ApiKey != null && ApiKey.Trim().EndsWith(":fx", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryParseResponse(string json, out DeepLResult result)
        {
            return JsonPayload.TryDeserialize(
                json,
                value => value != null &&
                    value.translations != null &&
                    value.translations.Length > 0 &&
                    value.translations[0] != null &&
                    !string.IsNullOrWhiteSpace(value.translations[0].text),
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
            Req.Headers.Add("Authorization", "DeepL-Auth-Key " + ApiKey);

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

        public class DeepLItem
        {
            public List<string> text { get; set; }
            public string target_lang { get; set; } = "";
            public string tag_handling { get; set; } = "html";
        }

        public class DeepLResult
        {
            public DeepLTranslation[] translations { get; set; }
        }

        public class DeepLTranslation
        {
            public string detected_source_language { get; set; }
            public string text { get; set; }
        }
    }
}