using RobloxUltimateScraper.Enums;

namespace RobloxUltimateScraper
{
    /// <summary>
    /// Range scraper
    /// </summary>
    internal class RangeScraper
    {
        public delegate void AssetDownloadFinished(bool success); // Once a version is finished downloading
        public delegate void AssetVersionsDiscovered(int versions); // Once asset scraper returns total asset versions
        public delegate void AssetFinished(); // Once an ID is finished downloading
        public delegate void AssetError(); // Once asset scraper setup errors

        public event AssetDownloadFinished? OnAssetDownloadFinished;
        public event AssetVersionsDiscovered? OnAssetVersionsDiscovered;
        public event AssetFinished? OnAssetFinished;
        public event AssetError? OnAssetError;

        public ulong StartRange { get; }
        public ulong EndRange { get; }

        public ulong CurrentId { get; private set; }

        public string OutputDirectory { get; }

        private object _lock = new object();

        public RangeScraper(ulong startRange, ulong endRange, string outputDirectory)
        {
            StartRange = startRange;
            EndRange = endRange;

            CurrentId = startRange - 1;

            OutputDirectory = outputDirectory;
        }

        public async Task StartWorker(HttpClient httpClient)
        {
            while (EndRange > CurrentId)
            {
                ulong id;
                lock (_lock)
                {
                    if (EndRange <= CurrentId)
                        continue;
                    CurrentId++;
                    id = CurrentId;
                }

                string outputDirectory = Path.Combine(OutputDirectory, $"Asset_{id}");
                if (Directory.Exists(outputDirectory) && File.Exists(Path.Combine(outputDirectory, "index.txt"))) // index.txt is an indication that the download was finished. does not work for non-index runs.
                {
                    Console.WriteLine($"Skipping {id} - already done. Delete the directory to redo the download.");
                    OnAssetFinished?.Invoke();
                    continue;
                }

                Scraper scraper = new Scraper(id, outputDirectory);
                var result = await scraper.Setup(httpClient);
                if (!result.Success)
                {
                    Console.WriteLine($"Failed to download {id} ({result.Message})");
                    OnAssetError?.Invoke();
                    continue;
                }
                OnAssetVersionsDiscovered?.Invoke(scraper.TotalVersions);

                scraper.OnDownloadFinished += (bool success) => OnAssetDownloadFinished?.Invoke(success);

                // do not bother compressing images gathered using this scraper (unless specified otherwise by the config)
                if (scraper.AssetType == AssetType.Image && !Config.Default.CompressImages)
                    scraper.ShouldCompress = false;

                await scraper.StartWorker(httpClient);

                scraper.PrintDownloadStatistics();
                scraper.WriteIndexFile();

                Console.WriteLine($"{id} has been completed.");
                OnAssetFinished?.Invoke();
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
    }
}
