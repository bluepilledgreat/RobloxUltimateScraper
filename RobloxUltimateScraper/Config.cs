using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// Output type
    /// </summary>
    internal enum OutputType
    {
        /// <summary>
        /// Asset files
        /// </summary>
        Files = 0,

        [Obsolete]
        FilesOnly = 0,

        /// <summary>
        /// Asset index
        /// </summary>
        Index = 1,

        [Obsolete]
        IndexOnly = 1,

        /// <summary>
        /// Console output
        /// </summary>
        Console = 2,

        /// <summary>
        /// Asset files and index
        /// </summary>
        Both = 3
    }

    /// <summary>
    /// Compression type on asset files
    /// </summary>
    internal enum CompressionType
    {
        /// <summary>
        /// No compression
        /// </summary>
        None,

        /// <summary>
        /// GZip compression
        /// </summary>
        GZip,

        /// <summary>
        /// BZip2 compression
        /// </summary>
        BZip2

        // TODO: zstd?
    }

    /// <summary>
    /// Index type
    /// </summary>
    internal enum IndexType
    {
        /// <summary>
        /// Text index
        /// </summary>
        Text,

        /// <summary>
        /// Json index
        /// </summary>
        Json,

        /// <summary>
        /// Text and json indexes
        /// </summary>
        All
    }

    /// <summary>
    /// Scraper type
    /// </summary>
    internal enum ScraperType
    {
        /// <summary>
        /// Asset version scraper
        /// </summary>
        Asset,

        /// <summary>
        /// Asset list scraper
        /// </summary>
        List,

        /// <summary>
        /// Asset list scraper, with versions
        /// </summary>
        ListVersions,

        /// <summary>
        /// Asset range scraper
        /// </summary>
        Range
    }

    /// <summary>
    /// Scraper configuration
    /// </summary>
    internal class Config
    {
        /// <summary>
        /// <see cref="Config"/> singleton.
        /// </summary>
        public static Config Default { get; set; } = default!;

        /// <summary>
        /// Selected scraper type.
        /// </summary>
        public ScraperType? Scraper { get; set; }

        /// <summary>
        /// Asset to scrape.
        /// Should be used with scraper types <see cref="ScraperType.Asset"/>.
        /// </summary>
        public long ScraperId { get; set; } = 0;

        /// <summary>
        /// Asset list to scrape.
        /// Should be used with scraper types <see cref="ScraperType.List"/> and <see cref="ScraperType.ListVersions"/>.
        /// </summary>
        public string ScraperListPath { get; set; } = string.Empty;

        /// <summary>
        /// Asset scrape start range.
        /// Should be used with scraper types <see cref="ScraperType.Range"/>.
        /// </summary>
        public long ScraperStartRange { get; set; } = 0;

        /// <summary>
        /// Asset scrape end range.
        /// Should be used with scraper types <see cref="ScraperType.Range"/>.
        /// </summary>
        public long ScraperEndRange { get; set; } = 0;

        /// <summary>
        /// Use the asset scraper.
        /// COMMAND LINE USE ONLY!
        /// </summary>
        [Option('a', "asset", Required = false, HelpText = "Use the asset scraper. Parameter takes in an ID.")]
        public long UseAssetScraper
        {
            set
            {
                Scraper = ScraperType.Asset;
                ScraperId = value;
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

                if (!long.TryParse(segments[0], out long startRange))
                    throw new ArgumentException("Start range is not an integer.");

                if (!long.TryParse(segments[1], out long endRange))
                    throw new ArgumentException("End range is not an integer.");

                ScraperStartRange = startRange;
                ScraperEndRange = endRange;
            }
        }

        /// <summary>
        /// Assets output type.
        /// </summary>
        [Option('o', "output", Required = false, Default = OutputType.Both, HelpText = "Assets output type.")]
        public OutputType OutputType { get; set; } = OutputType.Both;

        /// <summary>
        /// Index type.
        /// </summary>
        [Option('i', "index", Required = false, Default = IndexType.All, HelpText = "Index type.")]
        public IndexType IndexType { get; set; } = IndexType.All;

        /// <summary>
        /// Asset compression type.
        /// </summary>
        [Option('c', "compression", Required = false, Default = CompressionType.None, HelpText = "Asset compression type.")]
        public CompressionType CompressionType { get; set; } = CompressionType.None;

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
    }
}
