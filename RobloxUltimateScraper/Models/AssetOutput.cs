using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxUltimateScraper.Models
{
    /// <summary>
    /// Asset information for index
    /// </summary>
    internal class AssetOutput : IComparable<AssetOutput>
    {
        /// <summary>
        /// Asset version
        /// </summary>
        public long Id { get; set; }
        
        /// <summary>
        /// Asset version
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// CDN url
        /// </summary>
        public string? CDNUrl { get; set; }

        /// <summary>
        /// Asset size
        /// </summary>
        public double? FileSizeInMb { get; set; }

        /// <summary>
        /// Last modified date from CDN
        /// </summary>
        public string? LastModified { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string? Error { get; set; }

        private static string? GetCdnUrl(string? url, bool shouldTrim)
        {
            if (!shouldTrim || url == null)
                return url;

            // https://sc0.rbxcdn.com/hash?lotsofstuff
            int idx = url.IndexOf('?');
            if (idx != -1)
            {
                url = url[..(idx + 1)];
                url += "...";
            }

            return url;
        }

        // FORMAT
        // WITH ID:
        // 1818 | v1 | https://c0.rbxcdn.com/hash | March 18 2006 | 10Mb
        // WITH HASH:
        // hash | https://c0.rbxcdn.com/hash | March 18 2006 | 10Mb
        // WITH ERROR:
        // 1818 | v1 | Error: failed to download
        public string ToString(bool trimCdnUrl)
        {
            string output = $"{Id} | v{Version}";

            if (Error != null)
            {
                output += $" | Error: {Error}";
                return output;
            }

            if (CDNUrl != null)
                output += $" | {GetCdnUrl(CDNUrl, trimCdnUrl)}";

            if (LastModified != null)
                output += $" | {LastModified}";

            if (FileSizeInMb != null)
                output += $" | {FileSizeInMb}Mb";

            return output;
        }

        public override string ToString()
        {
            return ToString(trimCdnUrl: false);
        }

        public int CompareTo(AssetOutput? other)
        {
            if (other == null) return 1;

            // compare asset ids
            if (Id > other.Id) return 1;
            if (Id < other.Id) return -1;

            // compare versions
            if (Version > other.Version) return 1;
            if (Version < other.Version) return -1;

            return 0;
        }
    }
}
