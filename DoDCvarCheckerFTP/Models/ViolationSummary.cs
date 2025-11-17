using System;
using System.Collections.Generic;
using System.Linq;

namespace DoDCvarCheckerFTP.Models
{
    /// <summary>
    /// Aggregated violation summary for a single player across all servers
    /// </summary>
    public class ViolationSummary
    {
        /// <summary>
        /// Steam ID of the player
        /// </summary>
        public string SteamID { get; set; }

        /// <summary>
        /// All player names/aliases used by this Steam ID
        /// </summary>
        public HashSet<string> Aliases { get; set; } = new HashSet<string>();

        /// <summary>
        /// All IP addresses associated with this Steam ID
        /// </summary>
        public HashSet<string> IPAddresses { get; set; } = new HashSet<string>();

        /// <summary>
        /// Dictionary of cvar name -> count of violations
        /// </summary>
        public Dictionary<string, int> CvarViolations { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Total number of violations across all cvars
        /// </summary>
        public int TotalViolations => CvarViolations.Values.Sum();

        /// <summary>
        /// Number of unique cvars violated
        /// </summary>
        public int UniqueCvarsViolated => CvarViolations.Count;

        /// <summary>
        /// Most frequently violated cvar
        /// </summary>
        public string MostViolatedCvar
        {
            get
            {
                if (CvarViolations.Count == 0) return null;
                return CvarViolations.OrderByDescending(kvp => kvp.Value).First().Key;
            }
        }

        /// <summary>
        /// Count of the most violated cvar
        /// </summary>
        public int MostViolatedCount
        {
            get
            {
                if (CvarViolations.Count == 0) return 0;
                return CvarViolations.Values.Max();
            }
        }

        /// <summary>
        /// Add a violation to this summary
        /// </summary>
        public void AddViolation(string playerName, string ipAddress, string cvarName)
        {
            if (!string.IsNullOrWhiteSpace(playerName))
                Aliases.Add(playerName);

            if (!string.IsNullOrWhiteSpace(ipAddress))
                IPAddresses.Add(ipAddress);

            if (!string.IsNullOrWhiteSpace(cvarName))
            {
                if (!CvarViolations.ContainsKey(cvarName))
                    CvarViolations[cvarName] = 0;

                CvarViolations[cvarName]++;
            }
        }

        /// <summary>
        /// Get formatted string of all aliases
        /// </summary>
        public string GetAliasesString()
        {
            return string.Join("; ", Aliases);
        }

        /// <summary>
        /// Get formatted string of all IP addresses
        /// </summary>
        public string GetIPAddressesString()
        {
            return string.Join("; ", IPAddresses);
        }

        public override string ToString()
        {
            return $"{SteamID} - {TotalViolations} violations ({UniqueCvarsViolated} unique cvars)";
        }
    }
}
