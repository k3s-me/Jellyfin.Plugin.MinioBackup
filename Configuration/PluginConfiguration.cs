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
    }
}