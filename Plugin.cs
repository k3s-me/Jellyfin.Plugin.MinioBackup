using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MinioBackup
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "MinIO Backup Plugin";
        public override Guid Id => Guid.Parse("your-guid-here");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }
    }
}