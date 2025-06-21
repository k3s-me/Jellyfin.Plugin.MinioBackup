using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.MinioBackup.Configuration;

namespace Jellyfin.Plugin.MinioBackup
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "MinIO Backup Plugin";
        
        public override Guid Id => Guid.Parse("a4df60c5-6ab4-412a-8f79-cd28ec6f3bc6");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }
    }
}