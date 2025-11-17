using System;
using System.IO;
using System.Linq;
using System.Text;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Core.Reporting
{
    /// <summary>
    /// Generates violation reports in various formats
    /// Optimized for performance with StringBuilder and minimal allocations
    /// </summary>
    public class ReportGenerator
    {
        /// <summary>
        /// Generate a comprehensive text report file from violation data
        /// Compatible with original v1.0.0 format
        /// </summary>
        public static void GenerateTextReport(ViolationReport report, string outputPath)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

            // Ensure directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Use StringBuilder for efficient string building (avoids string concatenation in loops)
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
            sb.AppendLine($"  KTP CVAR VIOLATION REPORT");
            sb.AppendLine($"  Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Version: {report.AppVersion}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Summary Statistics
            sb.AppendLine("SUMMARY");
            sb.AppendLine("───────────────────────────────────────────────────────────────────────");
            sb.AppendLine($"  Unique Players:      {report.UniquePlayers}");
            sb.AppendLine($"  Total Violations:    {report.TotalViolationEntries:N0}");
            sb.AppendLine($"  Servers Checked:     {report.ServerCount}");
            sb.AppendLine($"  Log Lines Processed: {report.LogLinesProcessed:N0}");
            sb.AppendLine($"  Rates Ignored:       {(report.RatesIgnored ? "Yes" : "No")}");
            sb.AppendLine();
            sb.AppendLine();

            // Per-player details (sorted by total violations descending)
            sb.AppendLine("VIOLATIONS BY PLAYER (Sorted by Total Violations)");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
            sb.AppendLine();

            var sortedSummaries = report.GetSortedByViolations();

            foreach (var summary in sortedSummaries)
            {
                sb.AppendLine($"{summary.SteamID}");
                sb.AppendLine($"  Total Violations: {summary.TotalViolations}");
                sb.AppendLine($"  Unique CVARs: {summary.UniqueCvarsViolated}");
                sb.AppendLine();

                // Aliases
                if (summary.Aliases.Any())
                {
                    sb.AppendLine($"  Aliases: {summary.GetAliasesString()}");
                }

                // IP Addresses
                if (summary.IPAddresses.Any())
                {
                    sb.AppendLine($"  IP Addresses: {summary.GetIPAddressesString()}");
                }

                sb.AppendLine();

                // CVAR breakdown (sorted by count descending)
                sb.AppendLine("  CVAR Violations:");
                var sortedCvars = summary.CvarViolations.OrderByDescending(kvp => kvp.Value);
                foreach (var cvar in sortedCvars)
                {
                    sb.AppendLine($"    {cvar.Key}: {cvar.Value}");
                }

                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────────────────────────────");
                sb.AppendLine();
            }

            // Top violators summary
            sb.AppendLine();
            sb.AppendLine("TOP 10 OFFENDERS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");

            var topOffenders = report.GetTopOffenders(10);
            int rank = 1;
            foreach (var offender in topOffenders)
            {
                sb.AppendLine($"{rank}. {offender.SteamID} - {offender.TotalViolations} violations");
                if (offender.Aliases.Any())
                {
                    sb.AppendLine($"   Names: {string.Join(", ", offender.Aliases.Take(3))}");
                }
                rank++;
            }

            sb.AppendLine();
            sb.AppendLine();

            // Most violated CVARs
            sb.AppendLine("MOST VIOLATED CVARS");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");

            var allCvars = report.GetAllViolatedCvars();
            var cvarStats = new System.Collections.Generic.Dictionary<string, (int players, int total)>();

            foreach (var cvar in allCvars)
            {
                var stats = report.GetCvarStats(cvar);
                cvarStats[cvar] = stats;
            }

            var sortedCvarStats = cvarStats.OrderByDescending(kvp => kvp.Value.total).Take(20);

            foreach (var cvarStat in sortedCvarStats)
            {
                sb.AppendLine($"  {cvarStat.Key}:");
                sb.AppendLine($"    Players: {cvarStat.Value.players}, Total Violations: {cvarStat.Value.total}");
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Report generated by KTP Cvar Checker FTP {report.AppVersion}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════════");

            // Write to file
            File.WriteAllText(outputPath, sb.ToString());

            Console.WriteLine($"[ReportGenerator] Text report saved to: {outputPath}");
            Console.WriteLine($"[ReportGenerator] Report size: {new FileInfo(outputPath).Length:N0} bytes");
        }

        /// <summary>
        /// Generate a CSV report file from violation data
        /// </summary>
        public static void GenerateCSVReport(ViolationReport report, string outputPath)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

            // Ensure directory exists
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var sb = new StringBuilder();

            // CSV Header
            sb.AppendLine("SteamID,Aliases,IPAddresses,TotalViolations,UniqueCVARs,MostViolatedCVAR,MostViolatedCount");

            // Data rows
            foreach (var summary in report.Summaries)
            {
                string steamId = EscapeCSV(summary.SteamID);
                string aliases = EscapeCSV(summary.GetAliasesString());
                string ips = EscapeCSV(summary.GetIPAddressesString());
                int totalViolations = summary.TotalViolations;
                int uniqueCvars = summary.UniqueCvarsViolated;
                string mostViolated = EscapeCSV(summary.MostViolatedCvar ?? "");
                int mostViolatedCount = summary.MostViolatedCount;

                sb.AppendLine($"{steamId},{aliases},{ips},{totalViolations},{uniqueCvars},{mostViolated},{mostViolatedCount}");
            }

            File.WriteAllText(outputPath, sb.ToString());

            Console.WriteLine($"[ReportGenerator] CSV report saved to: {outputPath}");
        }

        /// <summary>
        /// Generate both text and CSV reports
        /// </summary>
        public static void GenerateAllReports(ViolationReport report, string baseFileName)
        {
            string textPath = $"{baseFileName}.txt";
            string csvPath = $"{baseFileName}.csv";

            GenerateTextReport(report, textPath);
            GenerateCSVReport(report, csvPath);
        }

        /// <summary>
        /// Escape CSV values (handle commas and quotes)
        /// </summary>
        private static string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        /// <summary>
        /// Generate a quick summary to console
        /// </summary>
        public static void PrintSummaryToConsole(ViolationReport report)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  CVAR VIOLATION REPORT SUMMARY");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"  Unique Players:   {report.UniquePlayers}");
            Console.WriteLine($"  Total Violations: {report.TotalViolationEntries:N0}");
            Console.WriteLine($"  Servers:          {report.ServerCount}");
            Console.WriteLine($"  Generated:        {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();

            if (report.UniquePlayers > 0)
            {
                Console.WriteLine("Top 5 Offenders:");
                var top5 = report.GetTopOffenders(5);
                int rank = 1;
                foreach (var offender in top5)
                {
                    Console.WriteLine($"  {rank}. {offender.SteamID} - {offender.TotalViolations} violations");
                    rank++;
                }
                Console.WriteLine();
            }
        }
    }
}

