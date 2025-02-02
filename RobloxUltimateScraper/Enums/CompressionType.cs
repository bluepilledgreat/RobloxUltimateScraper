namespace RobloxUltimateScraper.Enums
{
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
        BZip2,

        /// <summary>
        /// Zstd compression
        /// </summary>
        Zstd
    }
}
