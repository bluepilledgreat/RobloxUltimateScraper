using CommandLine;
using CommandLine.Text;
using RobloxUltimateScraper.Enums;
using RobloxUltimateScraper.Models;

namespace RobloxUltimateScraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0) // make it display the help menu
                args = new string[] { "--help" };

            Parser cmdLineParser = new Parser(settings => settings.CaseInsensitiveEnumValues = true);
            ParserResult<Config> configParser = cmdLineParser.ParseArguments<Config>(args);
            configParser.WithNotParsed(errors => Error(configParser, errors));
            await configParser.WithParsedAsync(async config => await Run(config));
        }

        /// <summary>
        /// Runs <see cref="RobloxUltimateScraper"/> with the specific configuration.
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns></returns>
        static async Task Run(Config config)
        {
            Config.Default = config;

            // TODO: add functionality for
            //       list
            //       list versions
            //       range
            switch (Config.Default.Scraper)
            {
                case null:
                    Console.WriteLine("Please define which scraper you wish to use!");
                    Console.WriteLine("Run the scraper with the --help argument for all commands.");
                    break;

                case ScraperType.Asset:
                    await RunAssetScraper();
                    break;

                case ScraperType.List:
                    Console.WriteLine("List scraper has not been implemented yet.");
                    break;

                case ScraperType.ListVersions:
                    Console.WriteLine("List versions scraper has not been implemented yet.");
                    break;

                case ScraperType.Range:
                    Console.WriteLine("Range scraper has not been implemented yet.");
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
        static void Error(ParserResult<Config> config, IEnumerable<Error> errors)
        {
            HelpText text = HelpText.AutoBuild(config);
            Console.WriteLine(text);
        }

        // TODO: move asset scraper to a separate file

        /// <summary>
        /// Sets the title for asset scraper
        /// </summary>
        /// <param name="id">Asset id</param>
        /// <param name="downloaded">Downloads</param>
        /// <param name="errors">Errors</param>
        /// <param name="total">Versions</param>
        static void SetAssetScraperTitle(long id, int downloaded, int errors, int total)
        {
            Console.Title = $"{nameof(RobloxUltimateScraper)} | Asset {id} | {downloaded}/{total} | {errors} Errors";
        }

        /// <summary>
        /// Starts the asset scraper
        /// </summary>
        /// <returns></returns>
        static async Task RunAssetScraper()
        {
            long assetId = Config.Default.ScraperId;

            if (string.IsNullOrEmpty(Config.Default.OutputDirectory) && !Scraper.ConsoleOnly)
                Config.Default.OutputDirectory = $"Asset_{assetId}";

            Scraper.ShouldTrimCdnUrlInConsole = Config.Default.TrimCdnUrlInConsole ?? !Scraper.ConsoleOnly;

            // get all place versions
            var assetDeliveryInfo = await Scraper.GetAssetDeliveryInformation(assetId);

            if (!assetDeliveryInfo.Success)
            {
                Console.WriteLine($"Failed to fetch versions for asset {assetId}: {assetDeliveryInfo.Error}");
                Environment.Exit(1);
            }

            Console.WriteLine($"Asset {assetId} has {assetDeliveryInfo.TotalVersions} versions!");

            Scraper.FileExtension = Config.Default.OutputExtension == "Auto" ? assetDeliveryInfo.AssetType.GetExtension() : Config.Default.OutputExtension;

            // add to queue
            for (int i = 1; i <= assetDeliveryInfo.TotalVersions; i++)
            {
                Scraper.Assets.Enqueue(new AssetInput
                {
                    Id = assetId,
                    Version = i
                });
            }

            // set up titles
            SetAssetScraperTitle(assetId, 0, 0, assetDeliveryInfo.TotalVersions);
            Scraper.OnDownloadFinished += () => SetAssetScraperTitle(assetId, Scraper.SuccessfulDownloads, Scraper.FailedDownloads, assetDeliveryInfo.TotalVersions);

            // start workers
            List<Task> workers = new List<Task>();

            for (int i = 1; i <= Config.Default.Workers; i++)
                workers.Add(Task.Run(Scraper.StartWorker));

            Task.WaitAll(workers.ToArray());

            // finalise
            Scraper.PrintDownloadStatistics();
            Scraper.WriteIndexFile($"{assetId} asset versions on {DateTime.Now.ToString("R")} ({assetDeliveryInfo.TotalVersions} versions)");
        }
    }
}