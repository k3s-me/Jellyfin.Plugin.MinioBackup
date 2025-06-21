using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MinioBackup.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string MinioEndpoint { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string BucketName { get; set; } = "jellyfin-backups";
        public bool UseSSL { get; set; } = true;
        public int BackupIntervalHours { get; set; } = 24;
        
        // Deze ontbraken in je code:
        public int RetentionDays { get; set; } = 30;
        public bool IncludeMetadata { get; set; } = false;
        public bool IncludeImages { get; set; } = false;
        public bool CompressBackups { get; set; } = true;
        public string[] ExcludePatterns { get; set; } = new[] 
        { 
            "transcodes/*", 
            "cache/*", 
            "*.tmp" 
        };
    }
}