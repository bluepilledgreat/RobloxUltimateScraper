using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// File utilities
    /// </summary>
    internal static class FileWriter
    {
        /// <summary>
        /// Constructs the output file name
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Output file name</returns>
        public static string BuildOutputFileName(string fileName)
        {
            return fileName + (!string.IsNullOrEmpty(Config.Default.OutputExtension) ? $".{Config.Default.OutputExtension}" : "");
        }

        /// <summary>
        /// Saves a stream with the given file path, last modified, and the configured compression type
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="stream">Stream</param>
        /// <param name="lastModified">Last modified</param>
        public static void Save(string filePath, Stream stream, DateTime? lastModified = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                switch (Config.Default.CompressionType)
                {
                    case CompressionType.GZip:
                        using (Ionic.Zlib.GZipStream compressor = new Ionic.Zlib.GZipStream(ms, Ionic.Zlib.CompressionMode.Compress, true))
                            stream.CopyTo(compressor);
                        filePath += ".gz";
                        break;
                    case CompressionType.BZip2:
                        using (Ionic.BZip2.BZip2OutputStream compressor = new Ionic.BZip2.BZip2OutputStream(ms, true))
                            stream.CopyTo(compressor);
                        filePath += ".bz2";
                        break;
                    default:
                        stream.CopyTo(ms);
                        break;
                }

                ms.Position = 0; // or else it wont write anything
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    ms.CopyTo(fileStream);

                if (lastModified.HasValue)
                    File.SetLastWriteTime(filePath, (DateTime)lastModified);
            }
        }
    }
}
