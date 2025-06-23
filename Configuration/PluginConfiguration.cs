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
            BucketName = "jellyfin";
            Region = "";
            UseSSL = true;
            RetentionDays = 30;
            BackupConfig = true;
            BackupPlugins = true;  
            BackupData = true;
            BackupLog = false;
            BackupMetadata = false;
            BackupRoot = false;
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
        
        /// <summary>Gets or sets the bucket name.</summary>
        public string Region { get; set; }
        
        /// <summary>Gets or sets a value indicating whether to use SSL.</summary>
        public bool UseSSL { get; set; }
        
        /// <summary>Gets or sets the retention days.</summary>
        public int RetentionDays { get; set; }
        
        /// <summary>Backup config folder.</summary>
        public bool BackupConfig { get; set; }
        
        /// <summary>Backup plugin folder.</summary>
        public bool BackupPlugins { get; set; } 
        
        /// <summary>Backup data folder.</summary>
        public bool BackupData { get; set; }
        
        /// <summary>Backup log folder.</summary>
        public bool BackupLog { get; set; }
        
        /// <summary>Backup metadata folder.</summary>
        public bool BackupMetadata { get; set; }
        
        /// <summary>Backup root folder.</summary>
        public bool BackupRoot { get; set; }
        
        /// <summary>Gets or sets the exclude patterns.</summary>
        public string[] ExcludePatterns { get; set; }
    }
}