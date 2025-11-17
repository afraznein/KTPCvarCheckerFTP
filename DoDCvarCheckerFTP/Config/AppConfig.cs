using System;
using System.IO;
using Newtonsoft.Json;

namespace DoDCvarCheckerFTP.Config
{
    /// <summary>
    /// Application-wide configuration settings
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Local sync directory path for files to upload
        /// </summary>
        [JsonProperty("localSyncPath")]
        public string LocalSyncPath { get; set; } = @"N:\Nein_\KTPCvarChecker\Sync\";

        /// <summary>
        /// Local logs directory path for downloaded logs
        /// </summary>
        [JsonProperty("localLogsPath")]
        public string LocalLogsPath { get; set; } = @"N:\Nein_\KTPCvarChecker\Logs\";

        /// <summary>
        /// Path to server configuration JSON file
        /// </summary>
        [JsonProperty("serverConfigPath")]
        public string ServerConfigPath { get; set; } = "servers.json";

        /// <summary>
        /// Maximum number of concurrent FTP connections
        /// </summary>
        [JsonProperty("maxConcurrentFTP")]
        public int MaxConcurrentFTP { get; set; } = 5;

        /// <summary>
        /// FTP operation timeout in seconds
        /// </summary>
        [JsonProperty("ftpTimeoutSeconds")]
        public int FTPTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Number of retry attempts for failed FTP operations
        /// </summary>
        [JsonProperty("ftpRetryAttempts")]
        public int FTPRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Deploy all map files flag
        /// </summary>
        [JsonProperty("deployAllMaps")]
        public bool DeployAllMaps { get; set; } = true;

        /// <summary>
        /// Deploy all sound files flag
        /// </summary>
        [JsonProperty("deployAllSounds")]
        public bool DeployAllSounds { get; set; } = false;

        /// <summary>
        /// Deploy all WAD files flag
        /// </summary>
        [JsonProperty("deployAllWads")]
        public bool DeployAllWads { get; set; } = true;

        /// <summary>
        /// Deploy latest KTP plugins flag
        /// </summary>
        [JsonProperty("deployLatestKTP")]
        public bool DeployLatestKTP { get; set; } = true;

        /// <summary>
        /// Ignore rate-related cvar violations when processing logs
        /// </summary>
        [JsonProperty("ignoreRates")]
        public bool IgnoreRates { get; set; } = false;

        /// <summary>
        /// SMTP server for email notifications
        /// </summary>
        [JsonProperty("smtpServer")]
        public string SmtpServer { get; set; }

        /// <summary>
        /// SMTP port
        /// </summary>
        [JsonProperty("smtpPort")]
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// SMTP username
        /// </summary>
        [JsonProperty("smtpUsername")]
        public string SmtpUsername { get; set; }

        /// <summary>
        /// SMTP password
        /// </summary>
        [JsonProperty("smtpPassword")]
        public string SmtpPassword { get; set; }

        /// <summary>
        /// Email sender address
        /// </summary>
        [JsonProperty("emailFrom")]
        public string EmailFrom { get; set; }

        /// <summary>
        /// Load application configuration from JSON file
        /// </summary>
        public static AppConfig LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Config file not found at {filePath}, using defaults");
                return new AppConfig();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load config: {ex.Message}. Using defaults.");
                return new AppConfig();
            }
        }

        /// <summary>
        /// Save application configuration to JSON file
        /// </summary>
        public void SaveToJson(string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Configuration saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Create default configuration file
        /// </summary>
        public static void CreateDefault(string filePath)
        {
            var config = new AppConfig();
            config.SaveToJson(filePath);
        }

        /// <summary>
        /// Validate that required paths exist
        /// </summary>
        public bool ValidatePaths()
        {
            bool valid = true;

            if (!Directory.Exists(LocalSyncPath))
            {
                Console.WriteLine($"WARNING: Sync path does not exist: {LocalSyncPath}");
                valid = false;
            }

            if (!Directory.Exists(LocalLogsPath))
            {
                Console.WriteLine($"WARNING: Logs path does not exist: {LocalLogsPath}");
                // Create it automatically
                try
                {
                    Directory.CreateDirectory(LocalLogsPath);
                    Console.WriteLine($"Created logs directory: {LocalLogsPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create logs directory: {ex.Message}");
                    valid = false;
                }
            }

            return valid;
        }
    }
}
