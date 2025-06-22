using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.MinioBackup.Configuration;

namespace Jellyfin.Plugin.MinioBackup
{
    /// <summary>
    /// MinIO Backup Plugin for Jellyfin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Gets the plugin name.
        /// </summary>
        public override string Name => "MinIO Backup Plugin";
        
        /// <summary>
        /// Gets the plugin unique identifier.
        /// </summary>
        public override Guid Id => Guid.Parse("a4df60c5-6ab4-412a-8f79-cd28ec6f3bc6");

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }
    }
}