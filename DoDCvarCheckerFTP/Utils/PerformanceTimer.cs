using System;
using System.Diagnostics;

namespace DoDCvarCheckerFTP.Utils
{
    /// <summary>
    /// Utility class for timing operations with automatic console output
    /// Implements IDisposable for easy using() block syntax
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly bool _verbose;

        /// <summary>
        /// Create a new performance timer and start timing
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <param name="verbose">Whether to print start message</param>
        public PerformanceTimer(string operationName, bool verbose = true)
        {
            _operationName = operationName;
            _verbose = verbose;
            _stopwatch = Stopwatch.StartNew();

            if (_verbose)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting: {_operationName}");
            }
        }

        /// <summary>
        /// Stop timing and print elapsed time
        /// </summary>
        public void Dispose()
        {
            _stopwatch.Stop();

            string elapsed = FormatElapsed(_stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Completed: {_operationName} in {elapsed}");
        }

        /// <summary>
        /// Get elapsed time in milliseconds without stopping the timer
        /// </summary>
        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        /// <summary>
        /// Format elapsed time in human-readable format
        /// </summary>
        private static string FormatElapsed(long milliseconds)
        {
            if (milliseconds < 1000)
            {
                return $"{milliseconds}ms";
            }
            else if (milliseconds < 60000)
            {
                return $"{milliseconds / 1000.0:F2}s";
            }
            else if (milliseconds < 3600000)
            {
                int minutes = (int)(milliseconds / 60000);
                int seconds = (int)((milliseconds % 60000) / 1000);
                return $"{minutes}m {seconds}s";
            }
            else
            {
                int hours = (int)(milliseconds / 3600000);
                int minutes = (int)((milliseconds % 3600000) / 60000);
                return $"{hours}h {minutes}m";
            }
        }
    }
}
