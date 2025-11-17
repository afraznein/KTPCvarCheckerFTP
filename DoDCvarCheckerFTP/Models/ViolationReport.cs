using System;
using System.Collections.Generic;
using System.Linq;

namespace DoDCvarCheckerFTP.Models
{
    /// <summary>
    /// Complete violation report containing aggregated data from all servers
    /// </summary>
    public class ViolationReport
    {
        /// <summary>
        /// List of violation summaries, one per unique Steam ID
        /// </summary>
        public List<ViolationSummary> Summaries { get; set; } = new List<ViolationSummary>();

        /// <summary>
        /// Total number of individual violation entries processed
        /// </summary>
        public int TotalViolationEntries { get; set; }

        /// <summary>
        /// Number of unique players with violations
        /// </summary>
        public int UniquePlayers => Summaries.Count;

        /// <summary>
        /// Number of servers logs were collected from
        /// </summary>
        public int ServerCount { get; set; }

        /// <summary>
        /// Number of log lines processed
        /// </summary>
        public int LogLinesProcessed { get; set; }

        /// <summary>
        /// When this report was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Whether rate violations were filtered out
        /// </summary>
        public bool RatesIgnored { get; set; }

        /// <summary>
        /// Version of the application that generated this report
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// Get top N offenders by total violations
        /// </summary>
        public List<ViolationSummary> GetTopOffenders(int count = 10)
        {
            return Summaries
                .OrderByDescending(s => s.TotalViolations)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get summaries sorted by total violations (descending)
        /// </summary>
        public List<ViolationSummary> GetSortedByViolations()
        {
            return Summaries
                .OrderByDescending(s => s.TotalViolations)
                .ToList();
        }

        /// <summary>
        /// Get all unique cvars that were violated across all players
        /// </summary>
        public List<string> GetAllViolatedCvars()
        {
            var cvars = new HashSet<string>();
            foreach (var summary in Summaries)
            {
                foreach (var cvar in summary.CvarViolations.Keys)
                {
                    cvars.Add(cvar);
                }
            }
            return cvars.OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Get statistics about a specific cvar across all players
        /// </summary>
        public (int PlayerCount, int TotalViolations) GetCvarStats(string cvarName)
        {
            int playerCount = 0;
            int totalViolations = 0;

            foreach (var summary in Summaries)
            {
                if (summary.CvarViolations.TryGetValue(cvarName, out int count))
                {
                    playerCount++;
                    totalViolations += count;
                }
            }

            return (playerCount, totalViolations);
        }

        public override string ToString()
        {
            return $"Violation Report: {UniquePlayers} players, {TotalViolationEntries} violations across {ServerCount} servers";
        }
    }
}
