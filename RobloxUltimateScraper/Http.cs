using System.Diagnostics;
using System.Net;

namespace RobloxUltimateScraper
{
    internal static class Http
    {
        private static CookieContainer _cookieContainer = null!;

        /// <summary>
        /// A singleton <see cref="HttpClient"/> that can be shared across all threads
        /// </summary>
        public static HttpClient Client { get; private set; } = null!;

        /// <summary>
        /// Boolean that indicates if a .ROBLOSECURITY cookie has been set for this session
        /// </summary>
        public static bool HasRobloxAuth { get; private set; } = false;

        /// <summary>
        /// Creates a new <see cref="HttpClient"/> with the appropriate settings for this scraper
        /// </summary>
        /// <returns>New HttpClient instance</returns>
        public static HttpClient CreateClient()
        {
            Debug.Assert(_cookieContainer != null);

            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false, // we are using v1 because v2 is bad
                CookieContainer = _cookieContainer,
                UseCookies = true
            };

            HttpClient client = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Config.Default.HttpTimeout)
            };
            client.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");

            return client;
        }

        private static string? GetRobloxAuthCookie()
        {
            if (!string.IsNullOrEmpty(Config.Default.AuthCookie))
            {
                Console.WriteLine("Using auth cookie from arguments.");
                return Config.Default.AuthCookie;
            }

            string? envValue;
            envValue = Environment.GetEnvironmentVariable("ROBLOXULTIMATESCRAPER_COOKIE");
            if (!string.IsNullOrEmpty(envValue))
            {
                Console.WriteLine("Using auth cookie from environment variables (ROBLOXULTIMATESCRAPER_COOKIE).");
                return envValue;
            }

            envValue = Environment.GetEnvironmentVariable("ROBLOXULTIMATESCRAPER_COOKIE_PATH");
            if (!string.IsNullOrEmpty(envValue))
            {
                Console.WriteLine("Using auth cookie from environment variables (ROBLOXULTIMATESCRAPER_COOKIE_PATH).");

                if (!File.Exists(envValue))
                    throw new ApplicationException($"Can not read the auth cookie: File {envValue} does not exist.");

                return File.ReadAllText(envValue);
            }

            return null;
        }

        public static void Init()
        {
            _cookieContainer = new CookieContainer();

            string? cookie = GetRobloxAuthCookie();
            if (cookie != null)
            {
                _cookieContainer.Add(new Cookie(".ROBLOSECURITY", cookie, "/", $".{Config.Default.BaseUrl}"));
                HasRobloxAuth = true;
            }

            Client = CreateClient();
        }
    }
}
