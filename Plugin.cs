using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.MinioBackup.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MinioBackup
{
    /// <summary>
    /// The main plugin class for the MinIO Backup plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly ILogger<Plugin> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Provides application path information.</param>
        /// <param name="xmlSerializer">Provides XML serialization services.</param>
        /// <param name="logger">Logger instance for diagnostics.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            _logger = logger;
            Instance = this;

            _logger.LogInformation("Plugin initialized with GUID: {PluginId}", Id);
            _logger.LogInformation("Configuration loaded: {ConfigType}", Configuration?.GetType().Name);
            _logger.LogInformation("Config file path: {ConfigPath}", ConfigurationFilePath);
        }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public override string Name => "MinIO Backup";

        /// <summary>
        /// Gets the unique ID of the plugin.
        /// </summary>
        public override Guid Id => Guid.Parse("a4df60c5-6ab4-412a-8f79-cd28ec6f3bc6");

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Gets the configuration page(s) for the plugin.
        /// </summary>
        /// <returns>List of plugin pages to expose to the Jellyfin UI.</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}.Configuration.configPage.html",
                        GetType().Namespace)
                }
            };
        }
    }
}
