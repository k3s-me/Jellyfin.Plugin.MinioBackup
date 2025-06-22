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

        public string MinioEndpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public bool UseSSL { get; set; }
        public int BackupIntervalHours { get; set; }
        public int RetentionDays { get; set; }
        public bool IncludeMetadata { get; set; }
        public bool IncludeImages { get; set; }
        public bool CompressBackups { get; set; }
        public string[] ExcludePatterns { get; set; }
    }
}