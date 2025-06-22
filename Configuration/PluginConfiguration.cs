using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MinioBackup.Configuration
{
    /// <summary>
    /// Configuration options for the MinIO Backup Plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the MinIO server endpoint (e.g., localhost:9000).
        /// </summary>
        public string MinioEndpoint { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the MinIO access key for authentication.
        /// </summary>
        public string AccessKey { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the MinIO secret key for authentication.
        /// </summary>
        public string SecretKey { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the MinIO bucket name where backups will be stored.
        /// </summary>
        public string BucketName { get; set; } = "jellyfin-backups";
        
        /// <summary>
        /// Gets or sets a value indicating whether to use SSL for MinIO connections.
        /// </summary>
        public bool UseSSL { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the backup interval in hours.
        /// </summary>
        public int BackupIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Gets or sets the number of days to retain backups before deletion.
        /// </summary>
        public int RetentionDays { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets a value indicating whether to include metadata in backups.
        /// </summary>
        public bool IncludeMetadata { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether to include images in backups.
        /// </summary>
        public bool IncludeImages { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value indicating whether to compress backups.
        /// </summary>
        public bool CompressBackups { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the file patterns to exclude from backups.
        /// </summary>
        public string[] ExcludePatterns { get; set; } = new[] 
        { 
            "transcodes/*", 
            "cache/*", 
            "*.tmp" 
        };
    }
}