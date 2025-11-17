using System;

namespace DoDCvarCheckerFTP.Models
{
    /// <summary>
    /// Represents connection information for a single game server
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Geographic region (e.g., "NewYork", "Chicago", "Dallas")
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Human-readable hostname (e.g., "1911-NY-1", "Thunder-CHI")
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// IP address of the FTP server
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// FTP port (typically 21)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// FTP username for authentication
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// FTP password for authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Whether this server is enabled for operations
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Optional description or notes about this server
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Display name combining region and hostname
        /// </summary>
        public string DisplayName => $"{Region}/{Hostname}";

        /// <summary>
        /// Validates that all required fields are populated
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Hostname) &&
                   !string.IsNullOrWhiteSpace(IP) &&
                   Port > 0 &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        public override string ToString()
        {
            return $"{DisplayName} ({IP}:{Port}) - {(Enabled ? "Enabled" : "Disabled")}";
        }
    }
}
