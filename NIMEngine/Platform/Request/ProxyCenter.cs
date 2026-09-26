using System.Net;

namespace NIMEngine.Request
{
    public class ProxyCenter
    {
        public static WebProxy CurrentProxy = null;

        public static void UsingProxy()
        {
            if (!string.IsNullOrWhiteSpace(NIM.Config.ProxyUrl))
            {
                WebProxy NewProxy = new WebProxy(NIM.Config.ProxyUrl);

                if (!string.IsNullOrEmpty(NIM.Config.ProxyUserName) &&
               !string.IsNullOrEmpty(NIM.Config.ProxyPassword))
                {
                    NewProxy.Credentials = new NetworkCredential(
                        NIM.Config.ProxyUserName,
                        NIM.Config.ProxyPassword
                    );
                }

                CurrentProxy = NewProxy;
            }
            else
            {
                CurrentProxy = null;
            }
        }
    }
}
