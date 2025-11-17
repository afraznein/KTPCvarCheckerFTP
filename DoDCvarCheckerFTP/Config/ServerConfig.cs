using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Config
{
    /// <summary>
    /// Configuration for all servers managed by the application
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// List of all configured servers
        /// </summary>
        [JsonProperty("servers")]
        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();

        /// <summary>
        /// Get only enabled servers
        /// </summary>
        [JsonIgnore]
        public List<ServerInfo> EnabledServers => Servers.Where(s => s.Enabled).ToList();

        /// <summary>
        /// Get servers by region
        /// </summary>
        public List<ServerInfo> GetServersByRegion(string region)
        {
            return Servers.Where(s => s.Region.Equals(region, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Get server by hostname
        /// </summary>
        public ServerInfo GetServerByHostname(string hostname)
        {
            return Servers.FirstOrDefault(s => s.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Load server configuration from JSON file
        /// </summary>
        public static ServerConfig LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Server configuration file not found: {filePath}");
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonConvert.DeserializeObject<ServerConfig>(json);

                // Validate all servers
                var invalidServers = config.Servers.Where(s => !s.IsValid()).ToList();
                if (invalidServers.Any())
                {
                    Console.WriteLine($"WARNING: Found {invalidServers.Count} invalid server configurations:");
                    foreach (var server in invalidServers)
                    {
                        Console.WriteLine($"  - {server.Hostname ?? "(no hostname)"}");
                    }
                }

                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse server configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save server configuration to JSON file
        /// </summary>
        public void SaveToJson(string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save server configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate all server configurations
        /// </summary>
        public bool ValidateAll()
        {
            return Servers.All(s => s.IsValid());
        }

        /// <summary>
        /// Get count of servers by region
        /// </summary>
        public Dictionary<string, int> GetRegionCounts()
        {
            return Servers
                .GroupBy(s => s.Region)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public override string ToString()
        {
            int enabled = EnabledServers.Count;
            int total = Servers.Count;
            return $"ServerConfig: {enabled}/{total} servers enabled";
        }
    }
}
