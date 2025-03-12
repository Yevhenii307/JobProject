using System;
using System.IO;
using System.Threading.Tasks;

namespace FolderSync
{
    public class Logger : IDisposable
    {
        private bool _disposed = false; // Flag to track whether the object has been disposed
        private StreamWriter _stream; // StreamWriter for writing logs to a file
        private string _path; // Path to the log file

        public string path
        {
            get { return _path; }
            set
            {
                _stream?.Dispose(); // Dispose the old stream if it exists
                _stream = new StreamWriter(value, true); // Open a new stream with append mode
                _path = value; // Update the file path
            }
        }

        public Logger(string logfile)
        {
            _path = Path.Combine(logfile); // Set the log file path
            _stream = new StreamWriter(_path, true); // Open the log file for writing (append mode)
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return; // Prevent multiple disposals
            if (disposing)
            {
                _stream?.Dispose(); // Dispose managed resources (StreamWriter)
                _stream = null;
            }
            _disposed = true; // Mark as disposed
        }

        public void Dispose()
        {
            Dispose(true); // Call the disposing method
            GC.SuppressFinalize(this); // Prevent garbage collector from calling finalizer
        }

        public async Task LogAsync(string msg)
        {
            Console.WriteLine(msg); // Print message to console
            await _stream.WriteLineAsync(msg).ConfigureAwait(false); // Write message to file asynchronously
            await _stream.FlushAsync().ConfigureAwait(false); // Flush the stream to ensure data is written
        }

        ~Logger()
        {
            Dispose(false); // Call Dispose with 'false' in case Dispose() was not called manually
        }
    }

    public static class LoggerSingleton
    {
        private static Logger _instance; // Singleton instance of Logger

        public static Logger GetLogger()
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("Logger instance is not set."); // Ensure instance is initialized
            }
            return _instance; // Return the existing instance
        }

        public static void SetLogger(Logger logger)
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("Logger instance is already set."); // Prevent multiple initializations
            }
            _instance = logger; // Set the singleton instance
        }
    }
}

