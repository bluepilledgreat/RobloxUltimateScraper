using System.Net;

namespace RobloxUltimateScraper
{
    internal static class Http
    {
        public static HttpClient Client { get; }

        static Http()
        {
            CookieContainer cookieContainer = new CookieContainer();
            string? cookie = null;

            if (!string.IsNullOrEmpty(Config.Default.AuthCookie))
            {
                Console.WriteLine("Using cookies from arguments.");
                cookie = Config.Default.AuthCookie;
            }
            else
            {
                string? envValue = Environment.GetEnvironmentVariable("ROBLOXULTIMATESCRAPER_COOKIE");
                if (!string.IsNullOrEmpty(envValue))
                {
                    Console.WriteLine("Using cookies from environment variables.");
                    cookie = envValue;
                }
            }

            if (cookie != null)
                cookieContainer.Add(new Cookie(".ROBLOSECURITY", cookie, "/", $".{Config.Default.BaseUrl}"));

            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false, // we are using v1 because v2 is bad
                CookieContainer = cookieContainer,
                UseCookies = true
            };

            Client = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Config.Default.HttpTimeout)
            };
            //_HttpClient.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinINet");
        }
    }
}
