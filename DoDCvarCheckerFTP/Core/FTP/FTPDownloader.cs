using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DoDCvarCheckerFTP.Config;
using DoDCvarCheckerFTP.Models;

namespace DoDCvarCheckerFTP.Core.FTP
{
    /// <summary>
    /// High-level FTP download operations for log files
    /// </summary>
    public class FTPDownloader
    {
        private readonly AppConfig _config;
        private readonly FTPManager _ftpManager;

        public FTPDownloader(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ftpManager = new FTPManager(config);
        }

        /// <summary>
        /// Download all log files from servers in parallel
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> DownloadCvarLogsAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var results = new Dictionary<ServerInfo, FTPOperationResult>();

            foreach (var server in servers)
            {
                var fileMapping = await BuildCvarLogFileMappingAsync(server);

                if (fileMapping.Count > 0)
                {
                    var serverList = new List<ServerInfo> { server };
                    var serverResults = await _ftpManager.DownloadFromServersAsync(serverList, fileMapping, progress);

                    if (serverResults.TryGetValue(server, out var result))
                    {
                        results[server] = result;
                    }
                }
                else
                {
                    Console.WriteLine($"[FTPDownloader] No cvar logs found on {server.DisplayName}");
                }
            }

            return results;
        }

        /// <summary>
        /// Download file consistency logs from servers
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> DownloadFileLogsAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var results = new Dictionary<ServerInfo, FTPOperationResult>();

            foreach (var server in servers)
            {
                var fileMapping = await BuildFileLogFileMappingAsync(server);

                if (fileMapping.Count > 0)
                {
                    var serverList = new List<ServerInfo> { server };
                    var serverResults = await _ftpManager.DownloadFromServersAsync(serverList, fileMapping, progress);

                    if (serverResults.TryGetValue(server, out var result))
                    {
                        results[server] = result;
                    }
                }
                else
                {
                    Console.WriteLine($"[FTPDownloader] No file logs found on {server.DisplayName}");
                }
            }

            return results;
        }

        /// <summary>
        /// Download DoD game logs from servers
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> DownloadDoDLogsAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var results = new Dictionary<ServerInfo, FTPOperationResult>();

            foreach (var server in servers)
            {
                var fileMapping = await BuildDoDLogFileMappingAsync(server);

                if (fileMapping.Count > 0)
                {
                    var serverList = new List<ServerInfo> { server };
                    var serverResults = await _ftpManager.DownloadFromServersAsync(serverList, fileMapping, progress);

                    if (serverResults.TryGetValue(server, out var result))
                    {
                        results[server] = result;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Delete cvar logs from servers after downloading
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> DeleteCvarLogsAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var remotePaths = new List<string>();

            // Build list of remote log file patterns
            // These will be discovered per-server
            var results = new Dictionary<ServerInfo, FTPOperationResult>();

            foreach (var server in servers)
            {
                var logFiles = await DiscoverCvarLogFilesAsync(server);

                if (logFiles.Count > 0)
                {
                    var serverList = new List<ServerInfo> { server };
                    var serverResults = await _ftpManager.DeleteFromServersAsync(serverList, logFiles, progress);

                    if (serverResults.TryGetValue(server, out var result))
                    {
                        results[server] = result;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Delete file logs from servers
        /// </summary>
        public async Task<Dictionary<ServerInfo, FTPOperationResult>> DeleteFileLogsAsync(
            List<ServerInfo> servers,
            IProgress<FTPProgress> progress = null)
        {
            var results = new Dictionary<ServerInfo, FTPOperationResult>();

            foreach (var server in servers)
            {
                var logFiles = await DiscoverFileLogFilesAsync(server);

                if (logFiles.Count > 0)
                {
                    var serverList = new List<ServerInfo> { server };
                    var serverResults = await _ftpManager.DeleteFromServersAsync(serverList, logFiles, progress);

                    if (serverResults.TryGetValue(server, out var result))
                    {
                        results[server] = result;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Build file mapping for cvar logs
        /// </summary>
        private async Task<Dictionary<string, string>> BuildCvarLogFileMappingAsync(ServerInfo server)
        {
            var mapping = new Dictionary<string, string>();

            try
            {
                using (var ftpClient = new FTPClient(server))
                {
                    await ftpClient.ConnectAsync();

                    // List log files in AMX logs directory
                    var remoteLogDir = "/dod/addons/amxmodx/logs";
                    var logFiles = await ftpClient.ListDirectoryAsync(remoteLogDir);

                    // Create local directory for this server's logs
                    string localServerDir = Path.Combine(_config.LocalLogsPath, server.Hostname);
                    Directory.CreateDirectory(localServerDir);

                    foreach (var remoteFile in logFiles)
                    {
                        // Only download .log files
                        if (remoteFile.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(remoteFile);
                            string localPath = Path.Combine(localServerDir, fileName);

                            mapping[remoteFile] = localPath;
                        }
                    }

                    await ftpClient.DisconnectAsync();
                }

                Console.WriteLine($"[FTPDownloader] Found {mapping.Count} cvar log files on {server.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTPDownloader] Error discovering logs on {server.DisplayName}: {ex.Message}");
            }

            return mapping;
        }

        /// <summary>
        /// Build file mapping for file consistency logs
        /// </summary>
        private async Task<Dictionary<string, string>> BuildFileLogFileMappingAsync(ServerInfo server)
        {
            var mapping = new Dictionary<string, string>();

            try
            {
                using (var ftpClient = new FTPClient(server))
                {
                    await ftpClient.ConnectAsync();

                    var remoteLogDir = "/dod/addons/amxmodx/logs";
                    var logFiles = await ftpClient.ListDirectoryAsync(remoteLogDir);

                    string localServerDir = Path.Combine(_config.LocalLogsPath, "FileChecks", server.Hostname);
                    Directory.CreateDirectory(localServerDir);

                    foreach (var remoteFile in logFiles)
                    {
                        // Look for filecheck-specific log files
                        if (remoteFile.Contains("filecheck") && remoteFile.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(remoteFile);
                            string localPath = Path.Combine(localServerDir, fileName);

                            mapping[remoteFile] = localPath;
                        }
                    }

                    await ftpClient.DisconnectAsync();
                }

                Console.WriteLine($"[FTPDownloader] Found {mapping.Count} file check logs on {server.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTPDownloader] Error discovering file logs on {server.DisplayName}: {ex.Message}");
            }

            return mapping;
        }

        /// <summary>
        /// Build file mapping for DoD game logs
        /// </summary>
        private async Task<Dictionary<string, string>> BuildDoDLogFileMappingAsync(ServerInfo server)
        {
            var mapping = new Dictionary<string, string>();

            try
            {
                using (var ftpClient = new FTPClient(server))
                {
                    await ftpClient.ConnectAsync();

                    var remoteLogDir = "/dod/logs";
                    var logFiles = await ftpClient.ListDirectoryAsync(remoteLogDir);

                    string localServerDir = Path.Combine(_config.LocalLogsPath, "DoDLogs", server.Hostname);
                    Directory.CreateDirectory(localServerDir);

                    foreach (var remoteFile in logFiles)
                    {
                        if (remoteFile.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileName = Path.GetFileName(remoteFile);
                            string localPath = Path.Combine(localServerDir, fileName);

                            mapping[remoteFile] = localPath;
                        }
                    }

                    await ftpClient.DisconnectAsync();
                }

                Console.WriteLine($"[FTPDownloader] Found {mapping.Count} DoD log files on {server.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTPDownloader] Error discovering DoD logs on {server.DisplayName}: {ex.Message}");
            }

            return mapping;
        }

        /// <summary>
        /// Discover cvar log files on a server for deletion
        /// </summary>
        private async Task<List<string>> DiscoverCvarLogFilesAsync(ServerInfo server)
        {
            var logFiles = new List<string>();

            try
            {
                using (var ftpClient = new FTPClient(server))
                {
                    await ftpClient.ConnectAsync();

                    var remoteLogDir = "/dod/addons/amxmodx/logs";
                    var files = await ftpClient.ListDirectoryAsync(remoteLogDir);

                    logFiles.AddRange(files);

                    await ftpClient.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTPDownloader] Error discovering logs for deletion on {server.DisplayName}: {ex.Message}");
            }

            return logFiles;
        }

        /// <summary>
        /// Discover file check log files on a server for deletion
        /// </summary>
        private async Task<List<string>> DiscoverFileLogFilesAsync(ServerInfo server)
        {
            var logFiles = new List<string>();

            try
            {
                using (var ftpClient = new FTPClient(server))
                {
                    await ftpClient.ConnectAsync();

                    var remoteLogDir = "/dod/addons/amxmodx/logs";
                    var files = await ftpClient.ListDirectoryAsync(remoteLogDir);

                    // Only filecheck logs
                    foreach (var file in files)
                    {
                        if (file.Contains("filecheck"))
                        {
                            logFiles.Add(file);
                        }
                    }

                    await ftpClient.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTPDownloader] Error discovering file logs for deletion on {server.DisplayName}: {ex.Message}");
            }

            return logFiles;
        }
    }
}
