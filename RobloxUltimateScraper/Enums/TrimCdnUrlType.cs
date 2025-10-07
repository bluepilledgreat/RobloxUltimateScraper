using System.Diagnostics;

namespace RobloxUltimateScraper.Enums
{
    internal enum TrimCdnUrlType
    {
        /// <summary>
        /// Turn off CDN url trimming entirely
        /// </summary>
        Off,

        /// <summary>
        /// Should trim the CDN url for Console only
        /// </summary>
        Console,

        /// <summary>
        /// Should trim the CDN url for Output only
        /// </summary>
        Output,

        /// <summary>
        /// Should trim the CDN url for both Console and Output
        /// </summary>
        All
    }

    internal static class TrimCdnUrlTypeEx
    {
        public static bool ShouldTrim(this TrimCdnUrlType type, OutputType outputType)
        {
            if (type == TrimCdnUrlType.Off)
                return false;
            if (type == TrimCdnUrlType.All)
                return true;

            switch (outputType)
            {
                case OutputType.Console:
                    return type == TrimCdnUrlType.Console;

                case OutputType.Index:
                    return type == TrimCdnUrlType.Output;

                default:
                    Debug.Assert(false);
                    return false;
            }
        }
    }
}
