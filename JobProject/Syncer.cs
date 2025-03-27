using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSync
{
    public class Syncer
    {
        private int _interval; // Time interval for synchronization
        private string _sourcePath; // Path to the source directory
        private string _replicaPath; // Path to the replica directory

        public Syncer((string source, string replica, string logfilepathint, int interval) args)
        {
            _sourcePath = args.source;
            _replicaPath = args.replica;
            _interval = args.interval;
        }

        // Starts the synchronization process and runs continuously until canceled
        public async Task StartSync(CancellationToken cancellationToken)
        {
            // Check if both directories exist before starting
            if (!Directory.Exists(_sourcePath) || !Directory.Exists(_replicaPath))
            {
                await LogAction("One of the directories does not exist.");
                return;
            };

            // Loop to continuously sync files until cancellation is requested
            while (!cancellationToken.IsCancellationRequested)
            {
                await SyncFolders(); // Perform synchronization
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_interval), cancellationToken); // Wait for the next sync cycle
                }
                catch (TaskCanceledException ex)
                {
                    await LogAction("The syncing task was canceled"); // Log cancellation message
                }
            }
        }

        // Logs the start and end of each sync cycle and calls the directory synchronization method
        private async Task SyncFolders()
        {
            await LogAction($"Sync started at {DateTime.Now}");
            try
            {
                await SynchronizeDirectories(_sourcePath, _replicaPath); // Perform file and directory synchronization
            }
            catch (Exception ex)
            {
                await LogAction($"Error during sync: {ex}"); // Log errors if any occur
            }
            await LogAction($"Sync finished at {DateTime.Now}");
        }

        // Synchronizes files and directories between source and replica
        private static async Task SynchronizeDirectories(string source, string replica)
        {
            var sourceFiles = new DirectoryInfo(source).GetFiles(); // Get all files from the source directory
            var replicaFiles = new DirectoryInfo(replica).GetFiles(); // Get all files from the replica directory

            // Copy new or updated files based on modification time and MD5 hash
            foreach (var file in sourceFiles)
            {
                string replicaFilePath = Path.Combine(replica, file.Name);
                if (!File.Exists(replicaFilePath))
                {
                    var replicaFile = new FileInfo(replicaFilePath);
                    if (file.LastWriteTime <= replicaFile.LastWriteTime &&
                        FilesAreEqual(file, new FileInfo(replicaFilePath)))
                    {
                        continue; // Skip if the file is already up to date
                    }
                    var result = file.CopyTo(replicaFilePath, true);
                    await LogAction($"Copied: {file.FullName} -> {result.FullName}"); // Log file copy action
                }
            }

            // Delete files in the replica that do not exist in the source
            foreach (var file in replicaFiles)
            {
                if (!sourceFiles.Any(f => f.Name == file.Name))
                {
                    file.Delete();
                    await LogAction($"Deleted: {file.FullName}"); // Log file deletion
                }
            }

            var sourceDirs = new DirectoryInfo(source).GetDirectories(); // Get all subdirectories in source
            var replicaDirs = new DirectoryInfo(replica).GetDirectories(); // Get all subdirectories in replica

            // Ensure all source directories exist in the replica and sync their contents
            foreach (var dir in sourceDirs)
            {
                string replicaDirPath = Path.Combine(replica, dir.Name);
                if (!Directory.Exists(replicaDirPath))
                {
                    var result = Directory.CreateDirectory(replicaDirPath);
                    await LogAction($"Created directory: {result.FullName}"); // Log directory creation
                }
                await SynchronizeDirectories(dir.FullName, replicaDirPath); // Recursively synchronize subdirectories
            }

            // Remove directories in the replica that do not exist in the source
            foreach (var dir in replicaDirs)
            {
                if (!sourceDirs.Any(d => d.Name == dir.Name))
                {
                    Directory.Delete(dir.FullName, true);
                    await LogAction($"Deleted directory: {dir.FullName}"); // Log directory deletion
                }
            }
        }

        // Compares two files using their names, sizes, and MD5 hash values
        private static bool FilesAreEqual(FileInfo file1, FileInfo file2)
        {
            return file1.Name == file2.Name && file1.Length == file2.Length && file1.CalculateMD5().SequenceEqual(file2.CalculateMD5());
        }

        // Logs messages to a singleton logger
        private static async Task LogAction(string message)
        {
            await LoggerSingleton.GetLogger().LogAsync(message);
        }
    }
}
