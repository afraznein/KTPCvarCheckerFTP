using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoDCvarCheckerFTP.Core.FTP
{
    /// <summary>
    /// Interface for FTP client operations
    /// </summary>
    public interface IFTPClient
    {
        /// <summary>
        /// Connect to the FTP server
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnect from the FTP server
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Upload a file to the FTP server
        /// </summary>
        /// <param name="localPath">Local file path</param>
        /// <param name="remotePath">Remote destination path</param>
        Task<bool> UploadFileAsync(string localPath, string remotePath);

        /// <summary>
        /// Download a file from the FTP server
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        /// <param name="localPath">Local destination path</param>
        Task<bool> DownloadFileAsync(string remotePath, string localPath);

        /// <summary>
        /// Delete a file on the FTP server
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        Task<bool> DeleteFileAsync(string remotePath);

        /// <summary>
        /// List files in a directory on the FTP server
        /// </summary>
        /// <param name="remotePath">Remote directory path</param>
        Task<List<string>> ListDirectoryAsync(string remotePath);

        /// <summary>
        /// Check if a file exists on the FTP server
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        Task<bool> FileExistsAsync(string remotePath);
    }
}
