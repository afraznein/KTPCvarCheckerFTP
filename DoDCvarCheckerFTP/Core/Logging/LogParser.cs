using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DoDCvarCheckerFTP.Models;
using DoDCvarCheckerFTP.Utils;

namespace DoDCvarCheckerFTP.Core.Logging
{
    /// <summary>
    /// Parses raw log lines into structured violation objects
    /// Optimized for performance with compiled regex and minimal allocations
    /// </summary>
    public class LogParser
    {
        // Compiled regex patterns for efficient parsing
        private static readonly Regex CvarViolationPattern = new Regex(
            @"STEAMID:(\d+:\d+:\d+)\s*\|\s*(.+?)\s*\|\s*(\d+\.\d+\.\d+\.\d+)\s*\|\s*Invalid\s+(.+?):\s*(.+?)\s*\(Required:\s*(.+?)\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex TimestampPattern = new Regex(
            @"L\s+(\d{1,2}/\d{1,2}/\d{4})\s+-\s+(\d{1,2}:\d{1,2}:\d{1,2}):",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Parse a single log line into a CvarViolation
        /// </summary>
        /// <param name="logLine">Raw log line from ktp_cvar.amxx</param>
        /// <param name="serverHostname">Server that generated this log</param>
        /// <returns>Parsed violation or null if line doesn't match pattern</returns>
        public static CvarViolation ParseCvarViolation(string logLine, string serverHostname = null)
        {
            if (string.IsNullOrWhiteSpace(logLine))
                return null;

            // Check if it's a KTP violation
            if (!StringCleaner.IsKTPViolation(logLine))
                return null;

            try
            {
                // Extract timestamp if present
                DateTime? timestamp = ExtractTimestamp(logLine);

                // Try to match the cvar violation pattern
                var match = CvarViolationPattern.Match(logLine);

                if (match.Success && match.Groups.Count >= 7)
                {
                    return new CvarViolation
                    {
                        SteamID = $"STEAMID:{match.Groups[1].Value.Trim()}",
                        PlayerName = match.Groups[2].Value.Trim(),
                        IPAddress = match.Groups[3].Value.Trim(),
                        CvarName = match.Groups[4].Value.Trim(),
                        InvalidValue = match.Groups[5].Value.Trim(),
                        RequiredValue = match.Groups[6].Value.Trim(),
                        Timestamp = timestamp ?? DateTime.Now,
                        ServerHostname = serverHostname,
                        RawLogLine = logLine
                    };
                }

                // Fallback: try simpler parsing
                return ParseCvarViolationSimple(logLine, serverHostname, timestamp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogParser] Error parsing line: {ex.Message}");
                Console.WriteLine($"[LogParser] Line: {logLine}");
                return null;
            }
        }

        /// <summary>
        /// Simpler parsing for lines that don't match the standard pattern
        /// </summary>
        private static CvarViolation ParseCvarViolationSimple(string logLine, string serverHostname, DateTime? timestamp)
        {
            // Split by pipe character
            var parts = logLine.Split('|');
            if (parts.Length < 3)
                return null;

            string steamId = StringCleaner.ExtractSteamID(parts[0]);
            if (string.IsNullOrEmpty(steamId))
                return null;

            string playerName = parts.Length > 1 ? parts[1].Trim() : "Unknown";
            string ip = StringCleaner.ExtractIPAddress(logLine);

            // Try to extract cvar info from the violation part
            string violationText = parts.Length > 2 ? parts[2] : "";
            string cvarName = "Unknown";
            string invalidValue = "Unknown";
            string requiredValue = "Unknown";

            if (violationText.Contains("Invalid"))
            {
                // Try to parse: "Invalid cvar_name: value (Required: value)"
                var parts2 = violationText.Split(new[] { "Invalid", ":", "(Required:", ")" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts2.Length >= 3)
                {
                    cvarName = parts2[0].Trim();
                    invalidValue = parts2[1].Trim();
                    requiredValue = parts2.Length > 2 ? parts2[2].Trim() : requiredValue;
                }
            }

            return new CvarViolation
            {
                SteamID = steamId,
                PlayerName = playerName,
                IPAddress = ip ?? "Unknown",
                CvarName = cvarName,
                InvalidValue = invalidValue,
                RequiredValue = requiredValue,
                Timestamp = timestamp ?? DateTime.Now,
                ServerHostname = serverHostname,
                RawLogLine = logLine
            };
        }

        /// <summary>
        /// Parse multiple log lines efficiently
        /// </summary>
        public static List<CvarViolation> ParseCvarViolations(List<string> logLines, string serverHostname = null)
        {
            var violations = new List<CvarViolation>(logLines.Count / 10); // Estimate ~10% are violations

            foreach (string line in logLines)
            {
                var violation = ParseCvarViolation(line, serverHostname);
                if (violation != null)
                {
                    violations.Add(violation);
                }
            }

            return violations;
        }

        /// <summary>
        /// Parse log lines from multiple servers
        /// </summary>
        public static Dictionary<string, List<CvarViolation>> ParseMultiServerLogs(
            Dictionary<string, List<string>> serverLogs)
        {
            var results = new Dictionary<string, List<CvarViolation>>();

            foreach (var kvp in serverLogs)
            {
                string serverName = kvp.Key;
                List<string> logs = kvp.Value;

                var violations = ParseCvarViolations(logs, serverName);
                results[serverName] = violations;
            }

            return results;
        }

        /// <summary>
        /// Extract timestamp from log line
        /// </summary>
        private static DateTime? ExtractTimestamp(string logLine)
        {
            var match = TimestampPattern.Match(logLine);
            if (!match.Success || match.Groups.Count < 3)
                return null;

            try
            {
                string dateStr = match.Groups[1].Value;
                string timeStr = match.Groups[2].Value;

                string combined = $"{dateStr} {timeStr}";

                if (DateTime.TryParse(combined, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
                // Ignore parse errors
            }

            return null;
        }

        /// <summary>
        /// Filter violations by criteria
        /// </summary>
        public static List<CvarViolation> FilterViolations(
            List<CvarViolation> violations,
            bool ignoreRates = false,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string steamIdFilter = null)
        {
            var filtered = violations;

            // Filter by rates
            if (ignoreRates)
            {
                filtered = filtered.FindAll(v => !v.IsRateViolation);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                filtered = filtered.FindAll(v => v.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filtered = filtered.FindAll(v => v.Timestamp <= endDate.Value);
            }

            // Filter by SteamID
            if (!string.IsNullOrEmpty(steamIdFilter))
            {
                filtered = filtered.FindAll(v =>
                    v.SteamID.Equals(steamIdFilter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered;
        }
    }
}

