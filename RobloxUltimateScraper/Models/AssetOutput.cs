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
        public long? Id { get; set; }
        
        /// <summary>
        /// Asset version
        /// </summary>
        public int? Version { get; set; }

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

        // FORMAT
        // WITH ID:
        // 1818 | v1 | https://c0.rbxcdn.com/hash | March 18 2006 | 10Mb
        // WITH HASH:
        // hash | https://c0.rbxcdn.com/hash | March 18 2006 | 10Mb
        // WITH ERROR:
        // 1818 | v1 | Error: failed to download
        public override string ToString()
        {
            string output = "";

            if (Id != null)
                output += $"{Id} | v{Version}";
            else
            {
                Debug.Assert(false);
                return "OUTPUT ERROR: NO ID OR HASH!";
            }

            if (Error != null)
            {
                output += $" | Error: {Error}";
                return output;
            }

            if (CDNUrl != null)
                output += $" | {CDNUrl}";

            if (LastModified != null)
                output += $" | {LastModified}";

            if (FileSizeInMb != null)
                output += $" | {FileSizeInMb}Mb";

            return output;
        }

        public int CompareTo(AssetOutput? other)
        {
            if (other == null) return 1;

            // asset id has priority over hashes
            if (other.Id == null && Id != null) return 1;
            if (other.Id != null && Id == null) return -1;

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
