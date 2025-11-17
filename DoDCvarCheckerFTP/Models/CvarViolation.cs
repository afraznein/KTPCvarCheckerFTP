using System;

namespace DoDCvarCheckerFTP.Models
{
    /// <summary>
    /// Represents a single client cvar violation detected by ktp_cvar.amxx
    /// </summary>
    public class CvarViolation
    {
        /// <summary>
        /// Steam ID of the player (e.g., "STEAMID:0:1:12345678")
        /// </summary>
        public string SteamID { get; set; }

        /// <summary>
        /// Player name at time of violation
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// IP address of the player
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Name of the cvar that was invalid
        /// </summary>
        public string CvarName { get; set; }

        /// <summary>
        /// The invalid value that was detected
        /// </summary>
        public string InvalidValue { get; set; }

        /// <summary>
        /// The required/expected value
        /// </summary>
        public string RequiredValue { get; set; }

        /// <summary>
        /// Timestamp when the violation was logged
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Server hostname where violation occurred
        /// </summary>
        public string ServerHostname { get; set; }

        /// <summary>
        /// Raw log line that was parsed
        /// </summary>
        public string RawLogLine { get; set; }

        /// <summary>
        /// Whether this is a rate-related cvar (cl_updaterate, cl_cmdrate, rate)
        /// </summary>
        public bool IsRateViolation
        {
            get
            {
                if (string.IsNullOrEmpty(CvarName)) return false;
                string lower = CvarName.ToLower();
                return lower.Contains("rate") ||
                       lower.Contains("interp") ||
                       lower.Contains("net_graph");
            }
        }

        public override string ToString()
        {
            return $"{SteamID} | {PlayerName} | {IPAddress} | {CvarName}: {InvalidValue} (Required: {RequiredValue})";
        }
    }
}
