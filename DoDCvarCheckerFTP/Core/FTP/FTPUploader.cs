using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DoDCvarCheckerFTP.Config;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Core.FTP
{
    /// <summary>
    /// High-level FTP upload operations with file discovery
    /// </summary>
    public class FTPUploader
    {
        private readonly AppConfig _config;
        private readonly FTPManager _ftpManager;

        public FTPUploader(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ftpManager = new FTPManager(config);
        }

        /// <summary>
        /// Build file mapping for KTP plugin deployment
        /// </summary>
        public Dictionary<string, string> BuildKTPPluginFileMapping()
        {
            var mapping = new Dictionary<string, string>();
            string syncPath = _config.LocalSyncPath;

            // AMX Configuration Files
            AddFile(mapping, syncPath, "amxmodx/configs/amxx.cfg", "/dod/addons/amxmodx/configs/amxx.cfg");
            AddFile(mapping, syncPath, "amxmodx/configs/plugins.ini", "/dod/addons/amxmodx/configs/plugins.ini");
            AddFile(mapping, syncPath, "amxmodx/configs/filelist.ini", "/dod/addons/amxmodx/configs/filelist.ini");

            // Language Files
            AddFile(mapping, syncPath, "amxmodx/data/lang/ktp_cvar.txt", "/dod/addons/amxmodx/data/lang/ktp_cvar.txt");
            AddFile(mapping, syncPath, "amxmodx/data/lang/ktp_cvarcfg.txt", "/dod/addons/amxmodx/data/lang/ktp_cvarcfg.txt");

            if (_config.DeployLatestKTP)
            {
                // KTP Plugins
                AddFile(mapping, syncPath, "amxmodx/plugins/ktp_cvar.amxx", "/dod/addons/amxmodx/plugins/ktp_cvar.amxx");
                AddFile(mapping, syncPath, "amxmodx/plugins/ktp_cvarconfig.amxx", "/dod/addons/amxmodx/plugins/ktp_cvarconfig.amxx");
                AddFile(mapping, syncPath, "amxmodx/plugins/filescheck.amxx", "/dod/addons/amxmodx/plugins/filescheck.amxx");
            }

            return mapping;
        }

        /// <summary>
        /// Build file mapping for WAD files
        /// </summary>
        public Dictionary<string, string> BuildWadFileMapping()
        {
            if (!_config.DeployAllWads)
            {
                return new Dictionary<string, string>();
            }

            var mapping = new Dictionary<string, string>();
            string syncPath = _config.LocalSyncPath;

            var wadFiles = new[]
            {
                "ace.wad",
                "dod_railroad.wad",
                "dod_railroad2_b2.wad",
                "dod_siena.wad",
                "jlord.wad",
                "lennonn.wad",
                "thunder2.wad",
                "cs_havana.wad",
                "dod_advance.wad",
                "dod_carta.wad"
            };

            foreach (var wad in wadFiles)
            {
                AddFile(mapping, syncPath, $"dod/{wad}", $"/dod/{wad}");
            }

            return mapping;
        }

        /// <summary>
        /// Build file mapping for map files
        /// </summary>
        public Dictionary<string, string> BuildMapFileMapping()
        {
            if (!_config.DeployAllMaps)
            {
                return new Dictionary<string, string>();
            }

            var mapping = new Dictionary<string, string>();
            string syncPath = _config.LocalSyncPath;
            string mapsDir = Path.Combine(syncPath, "dod", "maps");

            if (!Directory.Exists(mapsDir))
            {
                Console.WriteLine($"[FTPUploader] Maps directory not found: {mapsDir}");
                return mapping;
            }

            // Get all .bsp files from maps directory
            var mapFiles = Directory.GetFiles(mapsDir, "*.bsp");

            foreach (var mapFile in mapFiles)
            {
                string fileName = Path.GetFileName(mapFile);
                mapping[mapFile] = $"/dod/maps/{fileName}";
            }

            Console.WriteLine($"[FTPUploader] Found {mapping.Count} map files to deploy");

            return mapping;
        }

        /// <summary>
        /// Build file mapping for sound files
        /// </summary>
        public Dictionary<string, string> BuildSoundFileMapping()
        {
            if (!_config.DeployAllSounds)
            {
                return new Dictionary<string, string>();
            }

            var mapping = new Dictionary<string, string>();
            string syncPath = _config.LocalSyncPath;
            string soundDir = Path.Combine(syncPath, "dod", "sound");

            if (!Directory.Exists(soundDir))
            {
                Console.WriteLine($"[FTPUploader] Sound directory not found: {soundDir}");
                return mapping;
            }

            // Get all .wav files from sound directory and subdirectories
            var soundFiles = Directory.GetFiles(soundDir, "*.wav", SearchOption.AllDirectories);

            foreach (var soundFile in soundFiles)
            {
                // Get relative path from sound directory
                string relativePath = Path.GetRelativePath(soundDir, soundFile);
                string remotePath = $"/dod/sound/{relativePath.Replace("\\", "/")}";

                mapping[soundFile] = remotePath;
            }

            Console.WriteLine($"[FTPUploader] Found {mapping.Count} sound files to deploy");

            return mapping;
        }

        /// <summary>
        /// Build complete file mapping for full deployment
        /// </summary>
        public Dictionary<string, string> BuildCompleteFileMapping()
        {
            var mapping = new Dictionary<string, string>();

            // Merge all file mappings
            foreach (var kvp in BuildKTPPluginFileMapping())
                mapping[kvp.Key] = kvp.Value;

            foreach (var kvp in BuildWadFileMapping())
                mapping[kvp.Key] = kvp.Value;

            foreach (var kvp in BuildMapFileMapping())
                mapping[kvp.Key] = kvp.Value;

            foreach (var kvp in BuildSoundFileMapping())
                mapping[kvp.Key] = kvp.Value;

            Console.WriteLine($"[FTPUploader] Total files to deploy: {mapping.Count}");

            return mapping;
        }

        /// <summary>
        /// Upload all configured files to all servers
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> UploadToAllServersAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var fileMapping = BuildCompleteFileMapping();

            if (!fileMapping.Any())
            {
                Console.WriteLine("[FTPUploader] No files to upload");
                return new Dictionary<ServerInfo, FTPOperationResult>();
            }

            return await _ftpManager.UploadToServersAsync(servers, fileMapping, progress);
        }

        /// <summary>
        /// Upload only KTP plugins to all servers
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> UploadKTPPluginsOnlyAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var fileMapping = BuildKTPPluginFileMapping();
            return await _ftpManager.UploadToServersAsync(servers, fileMapping, progress);
        }

        /// <summary>
        /// Helper method to add file to mapping with validation
        /// </summary>
        private void AddFile(Dictionary<string, string> mapping, string syncPath, string relativePath, string remotePath)
        {
            string localPath = Path.Combine(syncPath, relativePath);

            if (File.Exists(localPath))
            {
                mapping[localPath] = remotePath;
            }
            else
            {
                Console.WriteLine($"[FTPUploader] Warning: File not found: {localPath}");
            }
        }
    }
}
