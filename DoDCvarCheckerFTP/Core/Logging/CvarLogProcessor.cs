using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DoDCvarCheckerFTP.Models;
using DoDCvarCheckerFTP.Config;
using DoDCvarCheckerFTP.Utils;

namespace DoDCvarCheckerFTP.Core.Logging
{
    /// <summary>
    /// High-level processor for cvar violation logs
    /// Aggregates violations and generates summary reports
    /// </summary>
    public class CvarLogProcessor
    {
        private readonly AppConfig _config;
        private readonly bool _ignoreRates;

        public CvarLogProcessor(AppConfig config, bool ignoreRates = false)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ignoreRates = ignoreRates;
        }

        /// <summary>
        /// Process all log files from local logs directory
        /// REPLACES the inefficient O(n³) processing from original code
        /// </summary>
        public ViolationReport ProcessAllLogs()
        {
            using (var timer = new PerformanceTimer("Processing all cvar logs"))
            {
                Console.WriteLine($"[CvarLogProcessor] Reading logs from: {_config.LocalLogsPath}");
                Console.WriteLine($"[CvarLogProcessor] Ignore rates: {_ignoreRates}");

                // Step 1: Read all log files (single pass)
                var allLogLines = ReadAllLogFiles();
                Console.WriteLine($"[CvarLogProcessor] Read {allLogLines.Count:N0} total log lines");

                // Step 2: Clean log lines (single pass, replaces 80+ LINQ operations)
                var cleanedLines = StringCleaner.ProcessLogLinesOptimized(allLogLines);
                Console.WriteLine($"[CvarLogProcessor] Cleaned to {cleanedLines.Count:N0} lines");

                // Step 3: Parse violations (single pass)
                var violations = LogParser.ParseCvarViolations(cleanedLines);
                Console.WriteLine($"[CvarLogProcessor] Found {violations.Count:N0} violations");

                // Step 4: Filter violations if needed
                if (_ignoreRates)
                {
                    violations = LogParser.FilterViolations(violations, ignoreRates: true);
                    Console.WriteLine($"[CvarLogProcessor] After rate filtering: {violations.Count:N0} violations");
                }

                // Step 5: Aggregate into report (single pass using dictionary)
                var report = AggregateViolations(violations);

                Console.WriteLine($"[CvarLogProcessor] Generated report:");
                Console.WriteLine($"  - Unique players: {report.UniquePlayers}");
                Console.WriteLine($"  - Total violations: {report.TotalViolationEntries}");
                Console.WriteLine($"  - Log lines processed: {report.LogLinesProcessed}");

                return report;
            }
        }

        /// <summary>
        /// Read all log files from logs directory
        /// Much more efficient than original file reading approach
        /// </summary>
        private List<string> ReadAllLogFiles()
        {
            var allLines = new List<string>();

            if (!Directory.Exists(_config.LocalLogsPath))
            {
                Console.WriteLine($"[CvarLogProcessor] Logs directory does not exist: {_config.LocalLogsPath}");
                return allLines;
            }

            // Get all .log files recursively
            var logFiles = Directory.GetFiles(_config.LocalLogsPath, "*.log", SearchOption.AllDirectories);

            Console.WriteLine($"[CvarLogProcessor] Found {logFiles.Length} log files");

            foreach (var logFile in logFiles)
            {
                try
                {
                    // Read all lines at once (more efficient than line-by-line)
                    var lines = File.ReadAllLines(logFile);
                    allLines.AddRange(lines);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CvarLogProcessor] Error reading {Path.GetFileName(logFile)}: {ex.Message}");
                }
            }

            return allLines;
        }

        /// <summary>
        /// Aggregate violations into summary report
        /// REPLACES the O(n³) dictionary access pattern from original code
        /// Uses TryGetValue for O(1) lookups instead of ElementAt(i) O(n) lookups
        /// </summary>
        private ViolationReport AggregateViolations(List<CvarViolation> violations)
        {
            var summaries = new Dictionary<string, ViolationSummary>();

            // Single pass aggregation - O(n) instead of O(n³)
            foreach (var violation in violations)
            {
                // Use TryGetValue for O(1) lookup instead of ContainsKey + indexer (2 lookups)
                if (!summaries.TryGetValue(violation.SteamID, out var summary))
                {
                    summary = new ViolationSummary
                    {
                        SteamID = violation.SteamID
                    };
                    summaries[violation.SteamID] = summary;
                }

                // Add violation details
                summary.AddViolation(
                    violation.PlayerName,
                    violation.IPAddress,
                    violation.CvarName
                );
            }

            // Build report
            var report = new ViolationReport
            {
                Summaries = summaries.Values.ToList(),
                TotalViolationEntries = violations.Count,
                LogLinesProcessed = violations.Count, // Simplified
                RatesIgnored = _ignoreRates,
                GeneratedAt = DateTime.Now,
                AppVersion = AppVersion.Full
            };

            return report;
        }

        /// <summary>
        /// Process logs from specific servers
        /// </summary>
        public Dictionary<string, ViolationReport> ProcessLogsByServer()
        {
            var results = new Dictionary<string, ViolationReport>();

            if (!Directory.Exists(_config.LocalLogsPath))
            {
                return results;
            }

            // Get server subdirectories
            var serverDirs = Directory.GetDirectories(_config.LocalLogsPath);

            foreach (var serverDir in serverDirs)
            {
                string serverName = Path.GetFileName(serverDir);

                try
                {
                    var logFiles = Directory.GetFiles(serverDir, "*.log");
                    var allLines = new List<string>();

                    foreach (var logFile in logFiles)
                    {
                        allLines.AddRange(File.ReadAllLines(logFile));
                    }

                    var cleanedLines = StringCleaner.ProcessLogLinesOptimized(allLines);
                    var violations = LogParser.ParseCvarViolations(cleanedLines, serverName);

                    if (_ignoreRates)
                    {
                        violations = LogParser.FilterViolations(violations, ignoreRates: true);
                    }

                    var report = AggregateViolations(violations);
                    report.ServerCount = 1;

                    results[serverName] = report;

                    Console.WriteLine($"[CvarLogProcessor] {serverName}: {violations.Count} violations, {report.UniquePlayers} players");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CvarLogProcessor] Error processing {serverName}: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// Get top offenders across all servers
        /// </summary>
        public List<ViolationSummary> GetTopOffenders(int count = 10)
        {
            var report = ProcessAllLogs();
            return report.GetTopOffenders(count);
        }

        /// <summary>
        /// Get violation statistics for a specific cvar
        /// </summary>
        public Dictionary<string, int> GetCvarStatistics()
        {
            var report = ProcessAllLogs();
            var cvarStats = new Dictionary<string, int>();

            foreach (var summary in report.Summaries)
            {
                foreach (var cvar in summary.CvarViolations)
                {
                    if (!cvarStats.ContainsKey(cvar.Key))
                    {
                        cvarStats[cvar.Key] = 0;
                    }
                    cvarStats[cvar.Key] += cvar.Value;
                }
            }

            return cvarStats.OrderByDescending(kvp => kvp.Value)
                           .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
