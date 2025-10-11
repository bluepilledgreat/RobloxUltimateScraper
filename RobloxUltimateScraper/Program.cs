using CommandLine;
using CommandLine.Text;
using RobloxUltimateScraper.Enums;
using System.Net;
using System.Reflection;

namespace RobloxUltimateScraper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) // make it display the help menu
                args = new string[] { "--help" };

            Parser cmdLineParser = new Parser(settings => settings.CaseInsensitiveEnumValues = true);
            ParserResult<CommandLineConfig> configParser = cmdLineParser.ParseArguments<CommandLineConfig>(args);
            configParser.WithNotParsed(errors => Error(configParser, errors));
            configParser.WithParsed(config => Run(config));
        }

        /// <summary>
        /// Runs <see cref="RobloxUltimateScraper"/> with the specific configuration.
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns></returns>
        static void Run(CommandLineConfig config)
        {
            Console.WriteLine($"RobloxUltimateScraper v{Assembly.GetExecutingAssembly().GetName().Version}");

            Config.Initialise(config);

            try
            {
                Http.Init();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: Failed to initialise HTTP ({ex})");
                Environment.Exit(1);
                return;
            }

            if (!Config.Default.DisableRobloxAuthChecks)
                CheckRobloxAuthStatus();

            // TODO: add functionality for
            //       list
            //       list versions
            switch (Config.Default.Scraper)
            {
                case ScraperType.None:
                    Console.WriteLine("No scraper selected.");
                    Console.WriteLine("Run the scraper with the --help argument for all commands.");
                    break;

                case ScraperType.Asset:
                    RunAssetScraper();
                    break;

                case ScraperType.List:
                    Console.WriteLine("List scraper has not been implemented yet.");
                    break;

                case ScraperType.ListVersions:
                    Console.WriteLine("List versions scraper has not been implemented yet.");
                    break;

                case ScraperType.Range:
                    RunRangeScraper();
                    break;

                default:
                    Console.WriteLine($"Unhandled scraper type {Config.Default.Scraper}!");
                    break;
            }
        }

        /// <summary>
        /// Handles command line parsing failure
        /// </summary>
        /// <param name="errors">Errors from command line parser</param>
        static void Error(ParserResult<CommandLineConfig> config, IEnumerable<Error> errors)
        {
            HelpText text = HelpText.AutoBuild(config);
            Console.WriteLine(text);
        }

        static void CheckRobloxAuthStatus()
        {
            RobloxAuthStatus status = GetRobloxAuthStatus(out int httpCode, out Exception? exception);

            switch (status)
            {
                case RobloxAuthStatus.Unauthenticated:
                    Console.WriteLine("[WARNING]: No authentication is set for this run. You may face problems downloading assets not authored by Roblox.");
                    break;

                case RobloxAuthStatus.InvalidAuth:
                    Console.WriteLine("[ERROR]: Provided authentication cookie is invalid.");
                    Environment.Exit(1);
                    return;

                case RobloxAuthStatus.Error:
                    if (exception != null)
                    {
                        Console.WriteLine($"[WARNING]: Failed to check if authentication is valid ({exception.Message}).");
                        Console.WriteLine(exception);
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING]: Failed to check if authentication is valid (got unexpected HTTP code {httpCode}).");
                    }
                    break;

            }

            if (status != RobloxAuthStatus.Authenticated && Config.Default.FailIfUnauthenticated)
            {
                Console.WriteLine("[ERROR]: Fail if unauthenticated flag is enabled. Stopping execution.");
                Environment.Exit(1);
                return;
            }
        }

        static RobloxAuthStatus GetRobloxAuthStatus(out int httpCode, out Exception? exception)
        {
            httpCode = 0;
            exception = null;

            if (!Http.HasRobloxAuth)
                return RobloxAuthStatus.Unauthenticated;

            try
            {
                HttpResponseMessage message = Http.Client.GetAsync("https://users.roblox.com/v1/users/authenticated").Result;

                httpCode = (int)message.StatusCode;

                return message.StatusCode switch
                {
                    HttpStatusCode.OK => RobloxAuthStatus.Authenticated,
                    HttpStatusCode.Unauthorized => RobloxAuthStatus.InvalidAuth,
                    _ => RobloxAuthStatus.Error
                };
            }
            catch (Exception ex)
            {
                exception = ex;
                return RobloxAuthStatus.Error;
            }
        }

        // TODO: move asset scraper to a separate file

        /// <summary>
        /// Sets the title for asset scraper
        /// </summary>
        /// <param name="id">Asset id</param>
        /// <param name="downloaded">Downloads</param>
        /// <param name="errors">Errors</param>
        /// <param name="total">Versions</param>
        static void SetAssetScraperTitle(ulong id, int downloaded, int errors, int total)
        {
            Console.Title = $"RobloxUltimateScraper | Asset {id} | {downloaded}/{total} | {errors} Errors";
        }

        /// <summary>
        /// Starts the asset scraper
        /// </summary>
        /// <returns></returns>
        static void RunAssetScraper()
        {
            ulong assetId = Config.Default.ScraperAssetId;

            string outputDirectory;

            if (string.IsNullOrEmpty(Config.Default.OutputDirectory))
                outputDirectory = $"Asset_{assetId}";
            else
                outputDirectory = Config.Default.OutputDirectory;

            Scraper scraper = new Scraper(assetId, outputDirectory);
            var setupResult = scraper.Setup(Http.Client).Result;
            if (!setupResult.Success)
            {
                Console.WriteLine(setupResult.Message);
                return;
            }

            if (Config.Default.ExpectedAssetType.HasValue && scraper.AssetType != Config.Default.ExpectedAssetType.Value)
            {
                Console.WriteLine($"Asset {assetId}'s type is not whitelisted (expected {Config.Default.ExpectedAssetType})");
                return;
            }

            Console.WriteLine($"Asset {assetId} has {scraper.TotalVersions} versions!");

            // set up titles
            SetAssetScraperTitle(assetId, 0, 0, scraper.TotalVersions);
            scraper.OnDownloadFinished += (_) => SetAssetScraperTitle(assetId, scraper.SuccessfulDownloads, scraper.FailedDownloads, scraper.TotalVersions);

            // start workers
            List<Task> workers = new List<Task>();

            for (int i = 1; i <= Config.Default.Workers; i++)
                workers.Add(Task.Run(scraper.StartWorker));

            Task.WaitAll(workers.ToArray());

            // finalise
            scraper.PrintDownloadStatistics();
            scraper.WriteIndexFile();
        }

        class RangeScraperData
        {
            public ulong StartRange = 0;
            public ulong EndRange = 0;

            public ulong TotalIds = 0;
            public int DownloadedIds = 0;
            public int ErrorIds = 0;

            public int TotalVersions = 0;
            public int DownloadedVersions = 0;
            public int ErrorVersions = 0;
        }

        static async Task RangeScraperTitleLogic(RangeScraperData data, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Console.Title = $"RobloxUltimateScraper | Range {data.StartRange}-{data.EndRange} | " +
                    $"{data.DownloadedIds}/{data.TotalIds} IDs ({data.ErrorIds} errors) | " +
                    $"{data.DownloadedIds}/{data.TotalVersions} Versions ({data.ErrorVersions} errors)";

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }    
        }

        static void RunRangeScraper()
        {
            string outputDirectory;

            if (string.IsNullOrEmpty(Config.Default.OutputDirectory))
                outputDirectory = $"Range_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            else
                outputDirectory = Config.Default.OutputDirectory;

            if (Config.Default.OutputType != OutputType.Console)
                Directory.CreateDirectory(outputDirectory);

            RangeScraperData data = new RangeScraperData()
            {
                StartRange = Config.Default.ScraperStartRange,
                EndRange = Config.Default.ScraperEndRange,
                TotalIds = Config.Default.ScraperEndRange - Config.Default.ScraperStartRange
            };

            RangeScraper scraper = new RangeScraper(Config.Default.ScraperStartRange, Config.Default.ScraperEndRange, outputDirectory);

            // set up titles
            scraper.OnAssetDownloadFinished += (bool success) => { Interlocked.Increment(ref data.DownloadedVersions); if (!success) { Interlocked.Increment(ref data.ErrorVersions); } };
            scraper.OnAssetVersionsDiscovered += (int versions) => { Interlocked.Add(ref data.TotalVersions, versions); };

            scraper.OnAssetFinished += () => Interlocked.Increment(ref data.DownloadedIds);
            scraper.OnAssetError += () => { Interlocked.Increment(ref data.DownloadedIds); Interlocked.Increment(ref data.ErrorIds); };

            CancellationTokenSource cts = new CancellationTokenSource();
            Task titleUpdateTask = Task.Run(() => RangeScraperTitleLogic(data, cts.Token));

            // start workers
            List<Task> workers = new List<Task>();

            for (int i = 1; i <= Config.Default.Workers; i++)
                workers.Add(Task.Run(scraper.StartWorker));

            Task.WaitAll(workers.ToArray());
            cts.Cancel();

            Console.WriteLine($"ID Range: {data.StartRange}-{data.EndRange}");
            Console.WriteLine($"Total IDs: {data.TotalIds}");
            Console.WriteLine($"Successful ID Downloads: {data.DownloadedIds}");
            Console.WriteLine($"Failed ID Downloads: {data.ErrorIds}");
            Console.WriteLine($"Total Versions: {data.TotalVersions}");
            Console.WriteLine($"Successful Version Downloads: {data.DownloadedVersions}");
            Console.WriteLine($"Failed Version Downloads: {data.ErrorVersions}");
        }
    }
}