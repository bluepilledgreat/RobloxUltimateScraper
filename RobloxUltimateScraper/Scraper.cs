using RobloxUltimateScraper.Enums;
using RobloxUltimateScraper.Models;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// Asset ID scraper
    /// </summary>
    internal class Scraper
    {
        /// <summary>
        /// Successful or failed download event.
        /// </summary>
        public delegate void DownloadFinished(bool success);

        private object _lock = new object();

        public ulong AssetId { get; }

        public int TotalVersions { get; private set; }

        public int CurrentVersion { get; private set; }

        public string OutputDirectory { get; }

        /// <summary>
        /// File extension to be used for saving
        /// </summary>
        public string? FileExtension { get; set; } = null;

        private int _successfulDownloads;
        /// <summary>
        /// Versions that successfully downloaded
        /// </summary>
        public int SuccessfulDownloads => _successfulDownloads;

        private int _failedDownloads;
        /// <summary>
        /// Versions that failed to download
        /// </summary>
        public int FailedDownloads => _failedDownloads;

        /// <summary>
        /// Event that fires upon a successful or failed download.
        /// </summary>
        public event DownloadFinished? OnDownloadFinished;

        /// <summary>
        /// Index entries
        /// </summary>
        private List<AssetOutput> _index = new List<AssetOutput>();

        /// <summary>
        /// Initialises values used by <see cref="Scraper"/>
        /// </summary>
        public Scraper(ulong assetId, string outputDirectory)
        {
            AssetId = assetId;
            OutputDirectory = outputDirectory;
        }

        public struct SetupResult
        {
            public bool Success;
            public string Message;
        }

        public async Task<SetupResult> Setup(HttpClient httpClient)
        {
            var assetDeliveryInfo = await GetAssetDeliveryInformation(httpClient);
            if (!assetDeliveryInfo.Success)
                return new SetupResult { Success = false, Message = $"Failed to fetch versions for asset {AssetId}: {assetDeliveryInfo.Error}" };

            TotalVersions = assetDeliveryInfo.TotalVersions;

            FileExtension = Config.Default.OutputExtension == "Auto" ? assetDeliveryInfo.AssetType.GetExtension() : Config.Default.OutputExtension;
            Directory.CreateDirectory(OutputDirectory);

            return new SetupResult { Success = true };
        }

        /// <summary>
        /// Creates a request to https://assetdelivery.roblox.com/v1/asset/
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <param name="version">Asset Version (0 for latest)</param>
        /// <returns>Http response</returns>
        public Task<HttpResponseMessage> AssetRequest(HttpClient httpClient, int version = 0)
        {
            string url = $"https://assetdelivery.{Config.Default.BaseUrl}/v1/asset/?id={AssetId}&version={version}";
            return httpClient.GetAsync(url);
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
        /// <returns>Asset delivery information</returns>
        public async Task<AssetDeliveryInformation> GetAssetDeliveryInformation(HttpClient httpClient)
        {
            HttpResponseMessage response = await AssetRequest(httpClient);

            if (response.StatusCode == HttpStatusCode.Conflict)
                return new AssetDeliveryInformation { Success = false, Error = "Insufficient permissions to download asset" };

            if (!IsSuccessStatusCode(response.StatusCode, allowForbidden: true)) // 403 means that the latest version is deleted but can still download
                return new AssetDeliveryInformation { Success = false, Error = $"Unhandled status code ({(int)response.StatusCode})" };

            IEnumerable<string>? values;
            int versions;
            AssetType assetType;

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
                {
                    Debug.Assert(false);
                    assetType = AssetType.Unknown;
                }
            }

            return new AssetDeliveryInformation { Success = true, TotalVersions = versions, AssetType = assetType };
        }

        /// <summary>
        /// Retrieves the CDN url from an asset id
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <param name="version">Version (0 for latest)</param>
        /// <returns>Success, Error string, CDN url</returns>
        public async Task<(bool, string, string)> GetCdnUrl(HttpClient httpClient, int version = 0)
        {
            try
            {
                HttpResponseMessage response = await AssetRequest(httpClient, version);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        return (false, "Insufficient permissions to download asset", "");

                    case HttpStatusCode.Forbidden:
                        return (false, "Asset version has been deleted", "");

                    case HttpStatusCode.TooManyRequests:
                        return (false, "Too many requests", "");
                }

                if (!IsSuccessStatusCode(response.StatusCode, allowForbidden: true)) // 403 means that the latest version is deleted but can still download
                    return (false, $"Unhandled status code ({(int)response.StatusCode}) ({await response.Content.ReadAsStringAsync()})", "");

                if (!response.Headers.TryGetValues("Location", out IEnumerable<string>? values))
                    return (false, "Location header is missing", ""); // this should never happen, but handle anyways

                string location = values.First();

                return (true, "Success", location);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString(), "");
            }
        }

        /// <summary>
        /// Gets content from the CDN using a specified URL
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <param name="url">CDN Url</param>
        /// <returns>Success, Error Message, HttpResponseMessage</returns>
        public static async Task<(bool, string, HttpResponseMessage?)> GetCdnContent(HttpClient httpClient, string url)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Forbidden:
                        return (false, "Asset not found on CDN", null);

                    case HttpStatusCode.TooManyRequests:
                        return (false, "Too many requests", null);

                    default:
                        if (!IsSuccessStatusCode(response.StatusCode))
                            return (false, $"Unknown status code ({(int)response.StatusCode})", null);
                        break;
                }

                return (true, "Success", response);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString(), null);
            }
        }

        /// <summary>
        /// Constructs the asset output path
        /// </summary>
        /// <param name="version">Version</param>
        /// <returns>Asset output path</returns>
        public string BuildAssetOutputFileName(int version)
        {
            string fileName = AssetId.ToString();
            if (version != 0)
                fileName += $"-v{version}";

            return fileName;
        }

        /// <summary>
        /// Logs an asset to index
        /// </summary>
        /// <param name="version">Version</param>
        /// <param name="cdnUrl">CDN url</param>
        /// <param name="fileSizeInMb">File size in Mb</param>
        /// <param name="lastModified">Last modified</param>
        /// <param name="error">Error message</param>
        private void LogAsset(
            int version,
            string? cdnUrl = null,
            double? fileSizeInMb = null,
            string? lastModified = null,
            string? error = null)
        {
            AssetOutput output = new AssetOutput
            {
                Version = version,
                CDNUrl = cdnUrl,
                FileSizeInMb = fileSizeInMb,
                LastModified = lastModified,
                Error = error
            };

            Console.WriteLine(output.ToString(trimCdnUrl: Config.Default.TrimCdnUrl.ShouldTrim(OutputType.Console)));

            _index.Add(output);
        }

        /// <summary>
        /// Logs an asset to index and saves it
        /// </summary>
        /// <param name="response">Http response messsage</param>
        /// <param name="version">Version</param>
        /// <param name="cdnUrl">CDN url</param>
        private async Task LogAssetFromCdnHttpMessageResponse(HttpResponseMessage response,
            int version,
            string cdnUrl)
        {
            // get last modified
            string? lastModified = null;
            if (response.Content.Headers.TryGetValues("last-modified", out IEnumerable<string>? lastModifiedValues))
                lastModified = lastModifiedValues.First();

            double? fileSize = null;

            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                fileSize = Math.Round(stream.Length / 1024.0 / 1024.0, 6);

                if (Config.Default.OutputType.IsFileSavingEnabled())
                {
                    string outputName = BuildAssetOutputFileName(version);
                    string path = Path.Combine(OutputDirectory, outputName);
                    string outputPath = FileWriter.BuildOutputFileName(path, FileExtension);

                    DateTime? lastModifiedDT = lastModified != null ? DateTime.Parse(lastModified) : null;

                    FileWriter.Save(outputPath, stream, Config.Default.CompressionLevel, lastModifiedDT);
                }
            }

            LogAsset(
                version: version,
                cdnUrl: cdnUrl,
                fileSizeInMb: fileSize,
                lastModified: lastModified
                );
        }

        /// <summary>
        /// Increments <see cref="SuccessfulDownloads"/> and invokes <see cref="OnDownloadFinished"/>.
        /// </summary>
        private void FireAssetSuccess()
        {
            Interlocked.Increment(ref _successfulDownloads);
            OnDownloadFinished?.Invoke(true);
        }

        /// <summary>
        /// Increments <see cref="FailedDownloads"/> and invokes <see cref="OnDownloadFinished"/>.
        /// </summary>
        private void FireAssetFailed()
        {
            Interlocked.Increment(ref _failedDownloads);
            OnDownloadFinished?.Invoke(false);
        }

        /// <summary>
        /// Creates a worker task
        /// </summary>
        /// <returns>Worker</returns>
        // TODO: add try catch blocks. give 3 retries w/ exceptions
        public async Task StartWorker(HttpClient httpClient)
        {
            while (TotalVersions > CurrentVersion)
            {
                int version;
                lock (_lock)
                {
                    if (TotalVersions <= CurrentVersion)
                        continue;
                    CurrentVersion++;
                    version = CurrentVersion;
                }

                // get the url
                (bool cdnGetSuccess, string cdnGetMessage, string cdnUrl) = await GetCdnUrl(httpClient, version);

                if (!cdnGetSuccess)
                {
                    LogAsset(error: $"Failed to fetch {AssetId} v{version}: {cdnGetMessage}", version: version);
                    FireAssetFailed();
                    continue;
                }

                // download the asset
                (bool cdnDownloadSuccess, string cdnDownloadMessage, HttpResponseMessage? cdnDownloadResponse) = await GetCdnContent(httpClient, cdnUrl);

                if (!cdnDownloadSuccess)
                {
                    LogAsset(error: $"Failed to fetch {AssetId} v{version} ({cdnUrl}): {cdnDownloadMessage}", version: version);
                    FireAssetFailed();
                    continue;
                }

                Debug.Assert(cdnDownloadResponse != null);

                // save!
                await LogAssetFromCdnHttpMessageResponse(cdnDownloadResponse, version, cdnUrl);
                FireAssetSuccess();
            }
        }

        public async Task StartWorker()
        {
            HttpClient httpClient = Config.Default.SingleHttpClient ? Http.Client : Http.CreateClient();

            try
            {
                await StartWorker(httpClient);
            }
            finally
            {
                // only dispose if it isnt the global http client
                if (httpClient != Http.Client)
                    httpClient.Dispose();
            }
        }

        /// <summary>
        /// Prints download statistics
        /// </summary>
        public void PrintDownloadStatistics()
        {
            Console.WriteLine($"{AssetId} | Successful Downloads: {SuccessfulDownloads}");
            Console.WriteLine($"{AssetId} | Failed Downloads: {FailedDownloads}");
            Console.WriteLine($"{AssetId} | Total Downloads: {SuccessfulDownloads + FailedDownloads}");
        }

        /// <summary>
        /// Writes the index file
        /// </summary>
        public void WriteIndexFile()
        {
            if (!Config.Default.OutputType.IsIndexEnabled())
                return;

            Directory.CreateDirectory(OutputDirectory);

            // sort index values
            _index.Sort();

            List<string> indexPaths = new List<string>();

            if (Config.Default.IndexType == IndexType.Text || Config.Default.IndexType == IndexType.All)
            {
                // create index file contents
                StringBuilder builder = new StringBuilder();

                builder.AppendLine($"{AssetId} asset versions on {DateTime.Now.ToString("R")} ({TotalVersions} versions)");

                foreach (AssetOutput asset in _index)
                    builder.AppendLine(asset.ToString(trimCdnUrl: Config.Default.TrimCdnUrl.ShouldTrim(OutputType.Index)));

                string contents = builder.ToString();

                string path = Path.Combine(OutputDirectory, "index.txt");
                indexPaths.Add(path);

                File.WriteAllText(path, contents);
            }

            if (Config.Default.IndexType == IndexType.Json || Config.Default.IndexType == IndexType.All)
            {
                string contents = JsonSerializer.Serialize(_index);

                string path = Path.Combine(OutputDirectory, "index.json");
                indexPaths.Add(path);

                File.WriteAllText(path, contents);
            }

            // write information about index
            Console.WriteLine($"Index file(s) can be found at {string.Join(", ", indexPaths)}");
        }
    }
}
