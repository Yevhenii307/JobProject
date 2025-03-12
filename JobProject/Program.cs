using System;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSync
{
    class Program
    {
        public static class CommandLineParser
        {
            // Returns a help message explaining the correct usage of the program
            public static string GetHelp()
            {
                return "Usage: FolderComparer.exe <FolderA> <FolderB> <LogFilePath> <IntervalInSeconds>";
            }

            // Parses command-line arguments and extracts folder paths, log file path, and delay interval
            public static bool TryParseCommandLineArgs(string[] args, out (string folder1, string folder2, string filepath, int delay) parsedParams)
            {
                parsedParams = (null, null, string.Empty, 0);

                // Validate that exactly 4 arguments are provided and the delay argument is a valid integer
                if (args.Length != 4 || !int.TryParse(args[3], out parsedParams.delay)) { return false; };

                parsedParams.folder1 = args[0]; // First folder path
                parsedParams.folder2 = args[1]; // Second folder path
                parsedParams.filepath = args[2]; // Log file path

                return true;
            }
        }

        static async Task Main(string[] args)
        {
            // Validate and parse input arguments
            if (CommandLineParser.TryParseCommandLineArgs(args, out var parsedParams) == false)
            {
                Console.WriteLine(CommandLineParser.GetHelp()); // Print help message if validation fails
                return;
            }

            var cts = new CancellationTokenSource(); // Token source for cancellation

            using (var logger = new Logger(parsedParams.filepath)) // Initialize logger with log file path
            {
                LoggerSingleton.SetLogger(logger); // Set the logger as a singleton

                // Handle Ctrl + C (SIGINT) to cancel synchronization
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("Ctrl + C pressed. Canceling sync.");
                    cts.Cancel(); // Signal cancellation to running tasks
                };

                Console.WriteLine("Press [Ctrl + C] to exit");

                var syncer = new Syncer(parsedParams); // Create an instance of Syncer with parsed parameters

                // Run the synchronization process in a separate task with cancellation support
                await Task.Run(async () => await syncer.StartSync(cts.Token), cts.Token);
            }
        }
    }
}
