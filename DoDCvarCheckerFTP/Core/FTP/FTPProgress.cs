namespace DoDCvarCheckerFTP.Core.FTP
{
    /// <summary>
    /// Progress information for FTP operations
    /// </summary>
    public class FTPProgress
    {
        /// <summary>
        /// Server hostname being processed
        /// </summary>
        public string ServerHostname { get; set; }

        /// <summary>
        /// Current file being processed
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// Current operation being performed (e.g., "Connecting", "Uploading", "Downloading")
        /// </summary>
        public string CurrentOperation { get; set; }

        /// <summary>
        /// Detailed message about the current operation
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Number of files completed
        /// </summary>
        public int FilesCompleted { get; set; }

        /// <summary>
        /// Total number of files to process
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Percentage complete (0-100)
        /// </summary>
        public double PercentComplete
        {
            get
            {
                if (TotalFiles == 0) return 0;
                return (double)FilesCompleted / TotalFiles * 100;
            }
        }

        /// <summary>
        /// Whether the operation completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                return $"{ServerHostname}: ERROR - {ErrorMessage}";
            }

            return $"{ServerHostname}: {FilesCompleted}/{TotalFiles} files ({PercentComplete:F1}%)";
        }
    }
}
