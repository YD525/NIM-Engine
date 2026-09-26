using System.Net;

namespace NIMEngine.Request
{
    public class ProxyCenter
    {
        public static WebProxy CurrentProxy = null;

        public static void UsingProxy()
        {
            if (!string.IsNullOrWhiteSpace(NIMEngine.Config.ProxyUrl))
            {
                WebProxy NewProxy = new WebProxy(NIMEngine.Config.ProxyUrl);

                if (!string.IsNullOrEmpty(NIMEngine.Config.ProxyUserName) &&
               !string.IsNullOrEmpty(NIMEngine.Config.ProxyPassword))
                {
                    NewProxy.Credentials = new NetworkCredential(
                        NIMEngine.Config.ProxyUserName,
                        NIMEngine.Config.ProxyPassword
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
