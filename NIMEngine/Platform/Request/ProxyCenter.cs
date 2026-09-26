using System.Net;

namespace NIMEngine.Request
{
    public class ProxyCenter
    {
        public static WebProxy CurrentProxy = null;

        public static void UsingProxy()
        {
            if (!string.IsNullOrWhiteSpace(NIM_Engine.Config.ProxyUrl))
            {
                WebProxy NewProxy = new WebProxy(NIM_Engine.Config.ProxyUrl);

                if (!string.IsNullOrEmpty(NIM_Engine.Config.ProxyUserName) &&
               !string.IsNullOrEmpty(NIM_Engine.Config.ProxyPassword))
                {
                    NewProxy.Credentials = new NetworkCredential(
                        NIM_Engine.Config.ProxyUserName,
                        NIM_Engine.Config.ProxyPassword
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
