using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MinioBackup.Configuration
{
    /// <summary>
    /// Configuration options for the MinIO Backup Plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            MinioEndpoint = "";
            AccessKey = "";
            SecretKey = "";
            BucketName = "jellyfin-backups";
            UseSSL = true;
            BackupIntervalHours = 24;
            RetentionDays = 30;
            IncludeMetadata = false;
            IncludeImages = false;
            CompressBackups = true;
            ExcludePatterns = new[] { "transcodes/*", "cache/*", "*.tmp" };
        }

        /// <summary>Gets or sets the MinIO endpoint.</summary>
        public string MinioEndpoint { get; set; }
        
        /// <summary>Gets or sets the access key.</summary>
        public string AccessKey { get; set; }
        
        /// <summary>Gets or sets the secret key.</summary>
        public string SecretKey { get; set; }
        
        /// <summary>Gets or sets the bucket name.</summary>
        public string BucketName { get; set; }
        
        /// <summary>Gets or sets a value indicating whether to use SSL.</summary>
        public bool UseSSL { get; set; }
        
        /// <summary>Gets or sets the backup interval hours.</summary>
        public int BackupIntervalHours { get; set; }
        
        /// <summary>Gets or sets the retention days.</summary>
        public int RetentionDays { get; set; }
        
        /// <summary>Gets or sets a value indicating whether to include metadata.</summary>
        public bool IncludeMetadata { get; set; }
        
        /// <summary>Gets or sets a value indicating whether to include images.</summary>
        public bool IncludeImages { get; set; }
        
        /// <summary>Gets or sets a value indicating whether to compress backups.</summary>
        public bool CompressBackups { get; set; }
        
        /// <summary>Gets or sets the exclude patterns.</summary>
        public string[] ExcludePatterns { get; set; }
    }
}