using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.MinioBackup.Services;

namespace Jellyfin.Plugin.MinioBackup.ScheduledTasks
{
    /// <summary>
    /// Scheduled task for creating complete MinIO backups (all folders).
    /// </summary>
    public class FullBackupTask : IScheduledTask
    {
        private readonly ILogger<FullBackupTask> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Gets the task name.
        /// </summary>
        public string Name => "MinIO Full Backup";
        
        /// <summary>
        /// Gets the task description.
        /// </summary>
        public string Description => "Creates a complete backup of ALL Jellyfin folders to MinIO (ignores folder selection settings)";
        
        /// <summary>
        /// Gets the task category.
        /// </summary>
        public string Category => "Backup";
        
        /// <summary>
        /// Gets the task key.
        /// </summary>
        public string Key => "MinioFullBackup";

        /// <summary>
        /// Initializes a new instance of the <see cref="FullBackupTask"/> class.
        /// </summary>
        public FullBackupTask(ILogger<FullBackupTask> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Executes the full backup task asynchronously.
        /// </summary>
        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            progress?.Report(0);

            try
            {
                var plugin = Plugin.Instance;
                if (plugin?.Configuration == null)
                {
                    _logger.LogError("Plugin configuration not found");
                    return;
                }

                var backupServiceLogger = _loggerFactory.CreateLogger<BackupService>();
                var backupService = new BackupService(backupServiceLogger, plugin.Configuration);
                
                // Force full backup (all folders)
                await backupService.CreateCompleteBackup();
                progress?.Report(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full backup task failed");
                throw;
            }
        }
        
        /// <summary>
        /// Gets the default triggers - weekly on Sunday at 3 AM.
        /// </summary>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerWeekly,
                    DayOfWeek = DayOfWeek.Sunday,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks // 3 AM on Sunday
                }
            };
        }
    }
}