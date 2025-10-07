using RobloxUltimateScraper.Enums;

namespace RobloxUltimateScraper
{
    internal class Config
    {
        public static Config Default { get; private set; } = null!;

        private CommandLineConfig _clConfig;

        public ulong ScraperAssetId => _clConfig.ScraperAssetId;

        public string ScraperListPath => _clConfig.ScraperListPath;

        public ulong ScraperStartRange => _clConfig.ScraperStartRange;
        public ulong ScraperEndRange => _clConfig.ScraperEndRange;

        public ScraperType Scraper => _clConfig.Scraper;
        public OutputType OutputType => _clConfig.OutputType;
        public IndexType IndexType => _clConfig.IndexType;

        public CompressionType CompressionType => _clConfig.CompressionType;
        public int CompressionLevel { get; }

        public string OutputDirectory => _clConfig.OutputDirectory;
        public string OutputExtension => _clConfig.OutputExtension;

        public int Workers => _clConfig.Workers;

        public string? AuthCookie => _clConfig.AuthCookie;

        public int HttpTimeout => _clConfig.HttpTimeout;

        public string BaseUrl => _clConfig.BaseUrl;

        public bool TrimCdnUrlInConsole { get; }

        /// <inheritdoc cref="CommandLineConfig.DisableRobloxAuthChecks"/>
        public bool DisableRobloxAuthChecks => _clConfig.DisableRobloxAuthChecks;

        /// <inheritdoc cref="CommandLineConfig.FailIfUnauthenticated"/>
        public bool FailIfUnauthenticated => _clConfig.FailIfUnauthenticated;

        /// <inheritdoc cref="CommandLineConfig.SingleHttpClient"/>
        public bool SingleHttpClient { get; }

        public Config(CommandLineConfig config)
        {
            _clConfig = config;

            CompressionLevel = _clConfig.CompressionLevelArgOtherName != null ? (int)_clConfig.CompressionLevelArgOtherName : _clConfig.CompressionLevelArg;
            TrimCdnUrlInConsole = _clConfig.TrimCdnUrlInConsole ?? OutputType != OutputType.Console;
            SingleHttpClient = _clConfig.SingleHttpClientOtherName ?? _clConfig.SingleHttpClient;
        }

        public static void Initialise(CommandLineConfig commandLineConfig)
        {
            if (Default != null)
                throw new Exception("Can not initialise Config twice.");

            Default = new Config(commandLineConfig);
        }
    }
}
