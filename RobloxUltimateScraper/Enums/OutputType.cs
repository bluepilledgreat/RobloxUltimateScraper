namespace RobloxUltimateScraper.Enums
{
    /// <summary>
    /// Asset output type
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
        /// Console output only
        /// </summary>
        Console = 2,

        /// <summary>
        /// Asset files and index
        /// </summary>
        Both = 3
    }

    internal static class OutputTypeEx
    {
        public static bool IsFileSavingEnabled(this OutputType type) => type == OutputType.Files || type == OutputType.Both;
        public static bool IsIndexEnabled(this OutputType type) => type == OutputType.Index || type == OutputType.Both;
    }
}
