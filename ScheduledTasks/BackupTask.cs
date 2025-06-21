using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.MinioBackup.Services;

namespace Jellyfin.Plugin.MinioBackup.ScheduledTasks
{
    public class BackupTask : IScheduledTask
    {
        private readonly ILogger<BackupTask> _logger;
        private readonly BackupService _backupService;

        public string Name => "MinIO Backup";
        public string Description => "Maakt een backup van Jellyfin data naar MinIO";
        public string Category => "Backup";
        public string Key => "MinioBackup";

        public BackupTask(ILogger<BackupTask> logger, BackupService backupService)
        {
            _logger = logger;
            _backupService = backupService;
        }

        // Probeer beide method signatures
        public async Task ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
        {
            progress?.Report(0);

            try
            {
                await _backupService.CreateFullBackup();
                progress?.Report(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup task gefaald");
                throw;
            }
        }

        // Fallback voor oudere Jellyfin versies
        public async Task Execute(CancellationToken cancellationToken, IProgress<double>? progress)
        {
            await ExecuteAsync(progress, cancellationToken);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks // 2 AM
                }
            };
        }
    }
}