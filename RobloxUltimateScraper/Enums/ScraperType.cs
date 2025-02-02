namespace RobloxUltimateScraper.Enums
{
    /// <summary>
    /// Scraper type
    /// </summary>
    internal enum ScraperType
    {
        /// <summary>
        /// No scraper selected
        /// </summary>
        None,

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
}
