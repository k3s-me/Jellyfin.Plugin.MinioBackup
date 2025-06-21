using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

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

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress?.Report(0);

            try
            {
                await _backupService.CreateBackup();
                progress?.Report(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup task gefaald");
                throw;
            }
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