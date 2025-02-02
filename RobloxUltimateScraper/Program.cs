using CommandLine;
using CommandLine.Text;
using RobloxUltimateScraper.Enums;
using System.Reflection;

namespace RobloxUltimateScraper
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            // TODO: add functionality for
            //       list
            //       list versions
            //       range
            switch (Config.Default.Scraper)
            {
                case ScraperType.None:
                    Console.WriteLine("No scraper chosen.");
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
        static void Error(ParserResult<CommandLineConfig> config, IEnumerable<Error> errors)
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

            if (string.IsNullOrEmpty(Config.Default.OutputDirectory) && Config.Default.OutputType != OutputType.Console)
                outputDirectory = $"Asset_{assetId}";
            else
                outputDirectory = Config.Default.OutputDirectory;

            Scraper scraper = new Scraper(assetId, outputDirectory);
            scraper.Setup().Wait();

            Console.WriteLine($"Asset {assetId} has {scraper.TotalVersions} versions!");

            // set up titles
            SetAssetScraperTitle(assetId, 0, 0, scraper.TotalVersions);
            scraper.OnDownloadFinished += () => SetAssetScraperTitle(assetId, scraper.SuccessfulDownloads, scraper.FailedDownloads, scraper.TotalVersions);

            // start workers
            List<Task> workers = new List<Task>();

            for (int i = 1; i <= Config.Default.Workers; i++)
                workers.Add(Task.Run(scraper.StartWorker));

            Task.WaitAll(workers.ToArray());

            // finalise
            scraper.PrintDownloadStatistics();
            scraper.WriteIndexFile($"{assetId} asset versions on {DateTime.Now.ToString("R")} ({scraper.TotalVersions} versions)");
        }
    }
}