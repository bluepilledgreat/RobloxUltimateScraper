namespace RobloxUltimateScraper.Enums
{
    internal enum MultipleDownloadStructureType
    {
        /// <summary>
        /// All asset downloads are put in their own directories
        /// </summary>
        Default,

        /// <summary>
        /// All asset download files and index files are in the same directories
        /// </summary>
        Combined,

        /// <summary>
        /// Asset download files and index files are put into their respective directories
        /// </summary>
        Separated
    }
}
