using System.Text.RegularExpressions;

namespace DoDCvarCheckerFTP.Utils
{
    /// <summary>
    /// Pre-compiled regex patterns for log processing
    /// Using RegexOptions.Compiled for better performance
    /// </summary>
    public static class RegexPatterns
    {
        /// <summary>
        /// Matches time format: HH:MM:SS:MS
        /// Example: "14:23:45:123"
        /// </summary>
        public static readonly Regex TimePattern =
            new Regex(@"\d+:\d+:\d+:\d*", RegexOptions.Compiled);

        /// <summary>
        /// Matches date format: MM/DD/YYYY
        /// Example: "11/17/2025"
        /// </summary>
        public static readonly Regex DatePattern =
            new Regex(@"\d+/\d+/\d+", RegexOptions.Compiled);

        /// <summary>
        /// Matches Steam ID format
        /// Example: "STEAMID:0:1:12345678"
        /// </summary>
        public static readonly Regex SteamIDPattern =
            new Regex(@"STEAMID:\d+:\d+:\d+", RegexOptions.Compiled);

        /// <summary>
        /// Matches IPv4 address
        /// Example: "192.168.1.1"
        /// </summary>
        public static readonly Regex IPPattern =
            new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", RegexOptions.Compiled);

        /// <summary>
        /// Matches log prefix format
        /// Example: "L 11/17/2025 - 14:23:45:"
        /// </summary>
        public static readonly Regex LogPrefixPattern =
            new Regex(@"^L\s+\d+/\d+/\d+\s+-\s+\d+:\d+:\d+:", RegexOptions.Compiled);
    }
}
