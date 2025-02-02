using RobloxUltimateScraper.Enums;
using RobloxUltimateScraper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// The core
    /// </summary>
    internal static class Scraper
    {
        /// <summary>
        /// Assets to download
        /// </summary>
        public static Queue<AssetInput> Assets { get; }

        /// <summary>
        /// File extension to be used for saving
        /// </summary>
        public static string? FileExtension { get; set; } = null;

        /// <summary>
        /// Is index enabled
        /// </summary>
        public static bool IndexEnabled { get { return Config.Default.OutputType == OutputType.Index || Config.Default.OutputType == OutputType.Both; } }

        /// <summary>
        /// Are files enabled
        /// </summary>
        public static bool FilesEnabled { get { return Config.Default.OutputType == OutputType.Files || Config.Default.OutputType == OutputType.Both; } }

        /// <summary>
        /// Is console only
        /// </summary>
        public static bool ConsoleOnly { get { return Config.Default.OutputType == OutputType.Console; } }

        /// <summary>
        /// Versions that successfully downloaded
        /// </summary>
        public static int SuccessfulDownloads { get; private set; }

        /// <summary>
        /// Versions that failed to download
        /// </summary>
        public static int FailedDownloads { get; private set; }

        /// <summary>
        /// Successful or failed download event.
        /// </summary>
        public delegate void DownloadFinished();

        /// <summary>
        /// Event that fires upon a successful or failed download.
        /// </summary>
        public static event DownloadFinished? OnDownloadFinished;

        /// <summary>
        /// <see cref="HttpClient"/> singleton.
        /// </summary>
        private static HttpClient _HttpClient { get; }

        /// <summary>
        /// Http client cookies.
        /// </summary>
        private static CookieContainer _CookieContainer { get; }

        /// <summary>
        /// Index entries
        /// </summary>
        private static List<AssetOutput> _Index { get; }

        /// <summary>
        /// Initialises values used by <see cref="Scraper"/>
        /// </summary>
        static Scraper()
        {
            Assets = new Queue<AssetInput>();

            SuccessfulDownloads = 0;
            FailedDownloads = 0;

            _CookieContainer = new CookieContainer();

            string? cookie = null;

            if (!string.IsNullOrEmpty(Config.Default.AuthCookie))
            {
                Console.WriteLine("Using cookies from arguments.");
                cookie = Config.Default.AuthCookie;
            }
            else
            {
                string? envValue = Environment.GetEnvironmentVariable("ROBLOXULTIMATESCRAPER_COOKIE");
                if (!string.IsNullOrEmpty(envValue))
                {
                    Console.WriteLine("Using cookies from environment variables.");
                    cookie = envValue;
                }
            }
            
            if (cookie != null)
                _CookieContainer.Add(new Cookie(".ROBLOSECURITY", cookie, "/", $".{Config.Default.BaseUrl}"));

            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false, // we are using v1 because v2 is bad
                CookieContainer = _CookieContainer,
                UseCookies = true
            };

            _HttpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(Config.Default.HttpTimeout)
            };
            //_HttpClient.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinINet");

            _Index = new List<AssetOutput>();
        }

        /// <summary>
        /// Creates a request to https://assetdelivery.roblox.com/v1/asset/
        /// </summary>
        /// <param name="id">Asset Id</param>
        /// <param name="version">Asset Version (0 for latest)</param>
        /// <returns>Http response</returns>
        public static Task<HttpResponseMessage> AssetRequest(long id, int version = 0)
        {
            string url = $"https://assetdelivery.{Config.Default.BaseUrl}/v1/asset/?id={id}&version={version}";
            return _HttpClient.GetAsync(url);
        }

        /// <summary>
        /// Check if status code is successful
        /// </summary>
        /// <param name="code">Http status code</param>
        /// <param name="allowForbidden">Should allow forbidden (403) responses</param>
        /// <returns>Successful status code</returns>
        public static bool IsSuccessStatusCode(HttpStatusCode code, bool allowForbidden = false)
        {
            switch (code)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.RedirectKeepVerb:
                    return true;
                case HttpStatusCode.Forbidden:
                    return allowForbidden;
                default:
                    return false;
            }
        }

        public struct AssetDeliveryInformation
        {
            public bool Success;
            public string Error;

            public int TotalVersions;
            public AssetType AssetType;
        }

        /// <summary>
        /// Retrieves information from asset delivery
        /// </summary>
        /// <param name="id">Asset Id</param>
        /// <returns>Asset delivery information</returns>
        public static async Task<AssetDeliveryInformation> GetAssetDeliveryInformation(long id)
        {
            HttpResponseMessage response = await AssetRequest(id);

            if (response.StatusCode == HttpStatusCode.Conflict)
                return new AssetDeliveryInformation { Success = false, Error = "Insufficient permissions to download asset" };

            if (!IsSuccessStatusCode(response.StatusCode, allowForbidden: true)) // 403 means that the latest version is deleted but can still download
                return new AssetDeliveryInformation { Success = false, Error = $"Unhandled status code ({(int)response.StatusCode})" };

            IEnumerable<string>? values;
            int versions = 0;
            AssetType assetType = 0;

            {
                if (!response.Headers.TryGetValues("roblox-assetversionnumber", out values))
                    return new AssetDeliveryInformation { Success = false, Error = "Asset version header is missing" }; // this should never happen, but handle anyways

                string versionsStr = values.First();

                if (!int.TryParse(versionsStr, out versions))
                    return new AssetDeliveryInformation { Success = false, Error = "Asset version header is non-numeric" }; // this should ALSO never happen, but handle anyways
            }

            {
                if (!response.Headers.TryGetValues("roblox-assettypeid", out values))
                    return new AssetDeliveryInformation { Success = false, Error = "Asset type ID header is missing" }; // this should never happen, but handle anyways

                string versionsStr = values.First();

                if (!Enum.TryParse(versionsStr, out assetType))
                    return new AssetDeliveryInformation { Success = false, Error = "Asset type ID header is invalid" }; // this should ALSO never happen, but handle anyways
            }

            return new AssetDeliveryInformation { Success = true, TotalVersions = versions, AssetType = assetType };
        }

        /// <summary>
        /// Retrieves the CDN url from an asset id
        /// </summary>
        /// <param name="id">Asset Id</param>
        /// <param name="version">Version (0 for latest)</param>
        /// <returns>Success, Error string, CDN url</returns>
        public static async Task<(bool, string, string)> GetCDNUrl(long id, int version = 0)
        {
            HttpResponseMessage response = await AssetRequest(id, version);

            if (response.StatusCode == HttpStatusCode.Conflict)
                return (false, "Insufficient permissions to download asset", "");

            if (!IsSuccessStatusCode(response.StatusCode, allowForbidden: true)) // 403 means that the latest version is deleted but can still download
                return (false, $"Unhandled status code ({(int)response.StatusCode}) ({await response.Content.ReadAsStringAsync()})", "");

            if (!response.Headers.TryGetValues("Location", out IEnumerable<string>? values))
                return (false, "Location header is missing", ""); // this should never happen, but handle anyways

            string location = values.First();

            return (true, "Success", location);
        }

        /// <summary>
        /// Constructs the asset output path
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="version">Version</param>
        /// <returns>Asset output path</returns>
        public static string BuildAssetOutputFileName(long id, int version)
        {
            string fileName = id.ToString();
            if (version != 0)
                fileName += $"-v{version}";

            return fileName;
        }

        /// <summary>
        /// Logs an asset to index
        /// </summary>
        /// <param name="id">Id</param>
        /// <param name="version">Version</param>
        /// <param name="cdnUrl">CDN url</param>
        /// <param name="fileSizeInMb">File size in Mb</param>
        /// <param name="lastModified">Last modified</param>
        /// <param name="error">Error message</param>
        private static void LogAsset(long id,
            int version,
            string? cdnUrl = null,
            double? fileSizeInMb = null,
            string? lastModified = null,
            string? error = null)
        {
            AssetOutput output = new AssetOutput
            {
                Id = id,
                Version = version,
                CDNUrl = cdnUrl,
                FileSizeInMb = fileSizeInMb,
                LastModified = lastModified,
                Error = error
            };

            Console.WriteLine(output);

            _Index.Add(output);
        }

        /// <summary>
        /// Logs an asset to index and saves it
        /// </summary>
        /// <param name="response">Http response messsage</param>
        /// <param name="id">Id</param>
        /// <param name="version">Version</param>
        /// <param name="cdnUrl">CDN url</param>
        private static async Task LogAssetFromCDNHttpMessageResponse(HttpResponseMessage response,
            long id,
            int version,
            string cdnUrl)
        {
            if (!ConsoleOnly)
                Directory.CreateDirectory(Config.Default.OutputDirectory);

            // get last modified
            string? lastModified = response.Content.Headers.GetValues("last-modified").FirstOrDefault();

            double? fileSize = null;

            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                fileSize = Math.Round(stream.Length / 1024f / 1024f, 6);

                if (FilesEnabled)
                {
                    string outputName = BuildAssetOutputFileName(id, version);
                    string path = Path.Combine(Config.Default.OutputDirectory, outputName);
                    string outputPath = FileWriter.BuildOutputFileName(path, FileExtension);

                    DateTime? lastModifiedDT = lastModified != null ? DateTime.Parse(lastModified) : null;

                    FileWriter.Save(outputPath, stream, lastModifiedDT);
                }
            }

            LogAsset(
                id: id,
                version: version,
                cdnUrl: cdnUrl,
                fileSizeInMb: fileSize,
                lastModified: lastModified
                );
        }

        /// <summary>
        /// Increments <see cref="SuccessfulDownloads"/> and invokes <see cref="OnDownloadFinished"/>.
        /// </summary>
        private static void FireAssetSuccess()
        {
            SuccessfulDownloads++;
            OnDownloadFinished?.Invoke();
        }

        /// <summary>
        /// Increments <see cref="FailedDownloads"/> and invokes <see cref="OnDownloadFinished"/>.
        /// </summary>
        private static void FireAssetFailed()
        {
            SuccessfulDownloads++;
            OnDownloadFinished?.Invoke();
        }

        /// <summary>
        /// Creates a worker task
        /// </summary>
        /// <returns>Worker</returns>
        // TODO: add try catch blocks. give 3 retries w/ exceptions
        public static async Task StartWorker()
        {
            while (Assets.Count > 0)
            {
                AssetInput asset;
                lock (Assets)
                {
                    if (Assets.Count == 0)
                        continue;
                    asset = Assets.Dequeue();
                }

                // get the url
                (bool cdnGetSuccess, string cdnGetMessage, string cdnUrl) = await GetCDNUrl(asset.Id, asset.Version);

                if (!cdnGetSuccess)
                {
                    LogAsset(error: $"Failed to fetch {asset.Id} v{asset.Version}: {cdnGetMessage}", id: asset.Id, version: asset.Version);
                    FireAssetFailed();
                    continue;
                }

                // download the asset
                HttpResponseMessage cdnResponse = await _HttpClient.GetAsync(cdnUrl);

                if (cdnResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    LogAsset(error: $"Failed to fetch {asset.Id} v{asset.Version} ({cdnUrl}): Asset not found on CDN", id: asset.Id, version: asset.Version);
                    FireAssetFailed();
                    continue;
                }

                if (!IsSuccessStatusCode(cdnResponse.StatusCode))
                {
                    LogAsset(error: $"Failed to fetch {asset.Id} v{asset.Version} ({cdnUrl}): Unknown status code ({(int)cdnResponse.StatusCode})", id: asset.Id, version: asset.Version);
                    FireAssetFailed();
                    continue;
                }

                // save!
                await LogAssetFromCDNHttpMessageResponse(cdnResponse, asset.Id, asset.Version, cdnUrl);
                FireAssetSuccess();
            }
        }

        /// <summary>
        /// Prints download statistics
        /// </summary>
        public static void PrintDownloadStatistics()
        {
            Console.WriteLine($"Successful Downloads: {SuccessfulDownloads}");
            Console.WriteLine($"Failed Downloads: {FailedDownloads}");
            Console.WriteLine($"Total Downloads: {SuccessfulDownloads + FailedDownloads}");
        }

        /// <summary>
        /// Writes the index file
        /// </summary>
        /// <param name="header">Index header</param>
        public static void WriteIndexFile(string header)
        {
            if (!IndexEnabled)
                return;

            Directory.CreateDirectory(Config.Default.OutputDirectory);

            // sort index values
            _Index.Sort();

            List<string> indexPaths = new List<string>();

            if (Config.Default.IndexType == IndexType.Text || Config.Default.IndexType == IndexType.All)
            {
                // create index file contents
                StringBuilder builder = new StringBuilder();

                builder.AppendLine(header);

                foreach (AssetOutput asset in _Index)
                    builder.AppendLine(asset.ToString());

                string contents = builder.ToString();

                string path = Path.Combine(Config.Default.OutputDirectory, "index.txt");
                indexPaths.Add(path);

                File.WriteAllText(path, contents);
            }

            if (Config.Default.IndexType == IndexType.Json || Config.Default.IndexType == IndexType.All)
            {
                string contents = JsonSerializer.Serialize(_Index);

                string path = Path.Combine(Config.Default.OutputDirectory, "index.json");
                indexPaths.Add(path);

                File.WriteAllText(path, contents);
            }

            // write information about index
            Console.WriteLine($"Index file(s) can be found at {string.Join(", ", indexPaths)}");
        }
    }
}
