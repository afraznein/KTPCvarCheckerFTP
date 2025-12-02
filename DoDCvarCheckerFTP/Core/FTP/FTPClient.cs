using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentFTP;
using DoDCvarCheckerFTP.Models;
using DoDCvarCheckerFTP.Utils;

namespace DoDCvarCheckerFTP.Core.FTP
{
    /// <summary>
    /// FluentFTP implementation of IFTPClient with retry logic
    /// </summary>
    public class FTPClient : IFTPClient, IDisposable
    {
        private readonly ServerInfo _serverInfo;
        private readonly int _timeoutSeconds;
        private readonly int _retryAttempts;
        private AsyncFtpClient _client;
        private bool _isConnected;

        public FTPClient(ServerInfo serverInfo, int timeoutSeconds = 300, int retryAttempts = 3)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
            _timeoutSeconds = timeoutSeconds;
            _retryAttempts = retryAttempts;

            if (!serverInfo.IsValid())
            {
                throw new ArgumentException("Invalid server configuration", nameof(serverInfo));
            }
        }

        /// <summary>
        /// Connect to the FTP server
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_isConnected && _client != null)
            {
                return true; // Already connected
            }

            try
            {
                return await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    _client = new AsyncFtpClient(
                        _serverInfo.IP,
                        _serverInfo.Username,
                        _serverInfo.Password,
                        _serverInfo.Port
                    );

                    _client.Config.ConnectTimeout = _timeoutSeconds * 1000;
                    _client.Config.DataConnectionConnectTimeout = _timeoutSeconds * 1000;
                    _client.Config.DataConnectionReadTimeout = _timeoutSeconds * 1000;

                    await _client.Connect();
                    _isConnected = true;

                    Console.WriteLine($"[FTP] Connected to {_serverInfo.DisplayName}");
                    return true;

                }, _retryAttempts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Failed to connect to {_serverInfo.DisplayName}: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the FTP server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_client != null && _isConnected)
            {
                try
                {
                    await _client.Disconnect();
                    Console.WriteLine($"[FTP] Disconnected from {_serverInfo.DisplayName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FTP] Error disconnecting from {_serverInfo.DisplayName}: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                }
            }
        }

        /// <summary>
        /// Upload a file to the FTP server with retry logic
        /// </summary>
        public async Task<bool> UploadFileAsync(string localPath, string remotePath)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to FTP server");
            }

            if (!File.Exists(localPath))
            {
                Console.WriteLine($"[FTP] Local file not found: {localPath}");
                return false;
            }

            try
            {
                return await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    var status = await _client.UploadFile(
                        localPath,
                        remotePath,
                        FtpRemoteExists.Overwrite,
                        createRemoteDir: true
                    );

                    bool success = status == FtpStatus.Success;

                    if (success)
                    {
                        Console.WriteLine($"[FTP] Uploaded: {Path.GetFileName(localPath)} → {remotePath}");
                    }
                    else
                    {
                        Console.WriteLine($"[FTP] Upload failed: {Path.GetFileName(localPath)} (Status: {status})");
                    }

                    return success;

                }, _retryAttempts, 2000); // 2 second initial delay
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Upload error for {Path.GetFileName(localPath)}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Download a file from the FTP server with retry logic
        /// </summary>
        public async Task<bool> DownloadFileAsync(string remotePath, string localPath)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to FTP server");
            }

            try
            {
                // Ensure local directory exists
                string directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                return await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    var status = await _client.DownloadFile(
                        localPath,
                        remotePath,
                        FtpLocalExists.Overwrite
                    );

                    bool success = status == FtpStatus.Success;

                    if (success)
                    {
                        Console.WriteLine($"[FTP] Downloaded: {remotePath} → {Path.GetFileName(localPath)}");
                    }
                    else
                    {
                        Console.WriteLine($"[FTP] Download failed: {remotePath} (Status: {status})");
                    }

                    return success;

                }, _retryAttempts, 2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Download error for {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a file on the FTP server
        /// </summary>
        public async Task<bool> DeleteFileAsync(string remotePath)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to FTP server");
            }

            try
            {
                await _client.DeleteFile(remotePath);
                Console.WriteLine($"[FTP] Deleted: {remotePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Delete error for {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List files in a directory on the FTP server
        /// </summary>
        public async Task<List<string>> ListDirectoryAsync(string remotePath)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to FTP server");
            }

            try
            {
                var items = await _client.GetListing(remotePath);
                return items
                    .Where(item => item.Type == FtpObjectType.File)
                    .Select(item => item.FullName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] List directory error for {remotePath}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Check if a file exists on the FTP server
        /// </summary>
        public async Task<bool> FileExistsAsync(string remotePath)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to FTP server");
            }

            try
            {
                return await _client.FileExists(remotePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] File exists check error for {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            if (_client != null)
            {
                try
                {
                    if (_isConnected)
                    {
                        // Use ConfigureAwait(false) to prevent potential deadlocks
                        _client.Disconnect().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    _client.Dispose();
                }
                catch (Exception ex)
                {
                    // Log exceptions during disposal for debugging
                    Console.WriteLine($"[FTPClient] Warning during disposal: {ex.Message}");
                }
                finally
                {
                    _client = null;
                    _isConnected = false;
                }
            }
        }
    }
}
