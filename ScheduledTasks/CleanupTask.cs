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
    /// Scheduled task for cleaning up old MinIO backups.
    /// </summary>
    public class CleanupTask : IScheduledTask
    {
        private readonly ILogger<CleanupTask> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Gets the task name.
        /// </summary>
        public string Name => "MinIO Backup Cleanup";
        
        /// <summary>
        /// Gets the task description.
        /// </summary>
        public string Description => "Removes old MinIO backups based on retention period";
        
        /// <summary>
        /// Gets the task category.
        /// </summary>
        public string Category => "Backup";
        
        /// <summary>
        /// Gets the task key.
        /// </summary>
        public string Key => "MinioBackupCleanup";

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanupTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{CleanupTask}"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        public CleanupTask(ILogger<CleanupTask> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Executes the cleanup task asynchronously.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
                
                await backupService.CleanupOldBackups();
                progress?.Report(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup cleanup task failed");
                throw;
            }
        }

        /// <summary>
        /// Executes the cleanup task (fallback for older Jellyfin versions).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double>? progress)
        {
            await ExecuteAsync(progress, cancellationToken);
        }

        /// <summary>
        /// Gets the default triggers for this task.
        /// </summary>
        /// <returns>The default triggers.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(4).Ticks // 4 AM
                }
            };
        }
    }
}