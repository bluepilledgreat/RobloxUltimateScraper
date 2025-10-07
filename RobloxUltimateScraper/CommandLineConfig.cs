using CommandLine;
using RobloxUltimateScraper.Enums;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// Scraper configuration
    /// </summary>
    internal class CommandLineConfig
    {
        /// <summary>
        /// Selected scraper type.
        /// </summary>
        public ScraperType Scraper { get; set; } = ScraperType.None;

        /// <summary>
        /// Asset to scrape.
        /// Should be used with scraper types <see cref="ScraperType.Asset"/>.
        /// </summary>
        public ulong ScraperAssetId { get; set; } = 0;

        /// <summary>
        /// Asset list to scrape.
        /// Should be used with scraper types <see cref="ScraperType.List"/> and <see cref="ScraperType.ListVersions"/>.
        /// </summary>
        public string ScraperListPath { get; set; } = string.Empty;

        /// <summary>
        /// Asset scrape start range.
        /// Should be used with scraper types <see cref="ScraperType.Range"/>.
        /// </summary>
        public ulong ScraperStartRange { get; set; } = 0;

        /// <summary>
        /// Asset scrape end range.
        /// Should be used with scraper types <see cref="ScraperType.Range"/>.
        /// </summary>
        public ulong ScraperEndRange { get; set; } = 0;

        /// <summary>
        /// Use the asset scraper.
        /// COMMAND LINE USE ONLY!
        /// </summary>
        [Option('a', "asset", Required = false, HelpText = "Use the asset scraper. Parameter takes in an ID.")]
        public ulong UseAssetScraper
        {
            set
            {
                Scraper = ScraperType.Asset;
                ScraperAssetId = value;
            }
        }

        /// <summary>
        /// Use the asset list scraper.
        /// COMMAND LINE USE ONLY!
        /// </summary>
        [Option('l', "list", Required = false, HelpText = "Use the asset list scraper. Parameter takes in a list path. WIP!")]
        public string UseListScraper
        {
            set
            {
                Scraper = ScraperType.List;
                ScraperListPath = value;
            }
        }

        /// <summary>
        /// Use the asset list versions scraper.
        /// COMMAND LINE USE ONLY!
        /// </summary>
        [Option("listversions", Required = false, HelpText = "Use the asset list version scraper. Parameter takes in a list path. WIP!")]
        public string UseListVersionsScraper
        {
            set
            {
                Scraper = ScraperType.ListVersions;
                ScraperListPath = value;
            }
        }

        /// <summary>
        /// Use the asset range scraper.
        /// COMMAND LINE USE ONLY!
        /// </summary>
        [Option('r', "range", Required = false, HelpText = "Use the asset range scraper. Parameter takes in [Start ID]-[End ID]. WIP!")]
        public string UseRangeScraper
        {
            set
            {
                Scraper = ScraperType.Range;

                // parse input
                string[] segments = value.Split('-');

                if (segments.Length != 2)
                    throw new ArgumentException("Parameter is not in valid format.");

                if (!ulong.TryParse(segments[0], out ulong startRange))
                    throw new ArgumentException("Start range is not an integer.");

                if (!ulong.TryParse(segments[1], out ulong endRange))
                    throw new ArgumentException("End range is not an integer.");

                ScraperStartRange = startRange;
                ScraperEndRange = endRange;
            }
        }

        /// <summary>
        /// Assets output type.
        /// </summary>
        [Option('o', "output", Required = false, Default = OutputType.Both, HelpText = "Assets output type. (Files, Index, Console, Both)")]
        public OutputType OutputType { get; set; } = OutputType.Both;

        /// <summary>
        /// Index type.
        /// </summary>
        [Option('i', "index", Required = false, Default = IndexType.All, HelpText = "Index type. (Text, Json, All)")]
        public IndexType IndexType { get; set; } = IndexType.All;

        /// <summary>
        /// Asset compression type.
        /// </summary>
        [Option('c', "compression", Required = false, Default = CompressionType.None, HelpText = "Compression type. (None, GZip, Bzip2, Zstd)")]
        public CompressionType CompressionType { get; set; } = CompressionType.None;

        [Option("compressionlevel", Required = false, Default = 9, HelpText = "Compression level for the compression. Only works for BZip2 (1-9) and Zstd (1-22). Other name: --cl.")]
        public int CompressionLevelArg { get; set; } = 9; // 9 is good for both BZip2 and Zstd

        // this sucks but commandlineparser has no way to set multiple names for an argument
        // and short arguments are only allowed to be a single character
        [Option("cl", Required = false, Hidden = true)]
        public int? CompressionLevelArgOtherName { get; set; }

        /// <summary>
        /// Assets output directory.
        /// </summary>
        [Option('d', "directory", Required = false, HelpText = "Assets output directory.")]
        public string OutputDirectory { get; set; } = "";

        /// <summary>
        /// Assets output extension.
        /// </summary>
        [Option('e', "extension", Required = false, Default = "Auto", HelpText = "Assets output extension. A value of 'Auto' will determine the extension based on the asset type.")]
        public string OutputExtension { get; set; } = "Auto";

        /// <summary>
        /// Number of scrape workers.
        /// </summary>
        [Option('w', "workers", Required = false, Default = 1, HelpText = "Number of scrape workers.")]
        public int Workers { get; set; } = 1;

        /// <summary>
        /// Roblox authentication cookie (ROBLOSECURITY).
        /// For copylocked game scraping.
        /// </summary>
        [Option("cookies", Required = false, HelpText = "Roblox authentication cookie (.ROBLOSECURITY). This argument is prioritised over the environment variable 'ROBLOXULTIMATESCRAPER_COOKIE'.")]
        public string? AuthCookie { get; set; }

        /// <summary>
        /// Http timeout in seconds.
        /// </summary>
        [Option('t', "timeout", Required = false, Default = 180, HelpText = "Http timeout in seconds.")]
        public int HttpTimeout { get; set; } = 180;

        private string _baseUrl = "roblox.com";

        /// <summary>
        /// Roblox environment to download from.
        /// </summary>
        [Option("baseurl", Required = false, Default = "www.roblox.com", HelpText = "Roblox environment to download from.")]
        public string BaseUrl
        {
            get => _baseUrl;

            set
            {
                if (value.StartsWith("http://"))
                    value = value[7..];
                else if (value.StartsWith("https://"))
                    value = value[8..];

                if (value.StartsWith("www.") || value.StartsWith("web."))
                    value = value[4..];

                int idx = value.IndexOf('/');
                if (idx != -1)
                    value = value[..idx];

                _baseUrl = value;
            }
        }

        [Option("trimcdnurlinconsole", Required = false, Default = null, HelpText = "Should the CDN url in console be trimmed.")]
        public bool? TrimCdnUrlInConsole { get; set; }

        /// <summary>
        /// Disables checks responsible for checking if the current run is authenticated.
        /// </summary>
        [Option("disablerobloxauthchecks", Required = false, Default = false, HelpText = "Disables checks responsible for checking if the current run is authenticated.")]
        public bool DisableRobloxAuthChecks { get; set; } = false;

        /// <summary>
        /// Should fail if the current run is unauthenticated?
        /// </summary>
        [Option("failifunauthenticated", Required = false, Default = false, HelpText = "Should fail if the current run is unauthenticated?")]
        public bool FailIfUnauthenticated { get; set; } = false;
    }
}
