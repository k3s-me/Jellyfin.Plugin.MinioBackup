using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using Minio;
using Minio.DataModel.Args;
using Jellyfin.Plugin.MinioBackup.Configuration;

namespace Jellyfin.Plugin.MinioBackup.Services
{
    public class BackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly PluginConfiguration _config;
        private readonly IMinioClient _minioClient;
        private readonly string _jellyfinDataPath;

        public BackupService(ILogger<BackupService> logger, PluginConfiguration config)
        {
            _logger = logger;
            _config = config;
            _jellyfinDataPath = GetJellyfinDataPath();

            _minioClient = new MinioClient()
                .WithEndpoint(_config.MinioEndpoint)
                .WithCredentials(_config.AccessKey, _config.SecretKey)
                .WithSSL(_config.UseSSL)
                .Build();
        }

        public async Task CreateFullBackup()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var tempBackupPath = Path.Combine(Path.GetTempPath(), $"jellyfin_backup_{timestamp}");

            try
            {
                Directory.CreateDirectory(tempBackupPath);

                // Maak backup van verschillende componenten
                await BackupConfigurations(tempBackupPath);
                await BackupDatabase(tempBackupPath);
                await BackupMetadata(tempBackupPath);
                await BackupPlugins(tempBackupPath);

                // Comprimeer alles
                var zipPath = $"{tempBackupPath}.zip";
                ZipFile.CreateFromDirectory(tempBackupPath, zipPath);

                // Upload naar MinIO
                await UploadToMinio(zipPath, $"full_backup_{timestamp}.zip");

                _logger.LogInformation($"Volledige backup voltooid: {zipPath}");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempBackupPath))
                    Directory.Delete(tempBackupPath, true);
            }
        }

        private async Task BackupConfigurations(string backupPath)
        {
            var configBackupPath = Path.Combine(backupPath, "config");
            Directory.CreateDirectory(configBackupPath);

            var configPath = Path.Combine(_jellyfinDataPath, "config");

            if (Directory.Exists(configPath))
            {
                await CopyDirectoryAsync(configPath, configBackupPath, new[]
                {
                    "*.xml",
                    "*.json",
                    "*.conf"
                });
            }
        }

        private async Task BackupDatabase(string backupPath)
        {
            var dbBackupPath = Path.Combine(backupPath, "data");
            Directory.CreateDirectory(dbBackupPath);

            var dataPath = Path.Combine(_jellyfinDataPath, "data");

            if (Directory.Exists(dataPath))
            {
                // Backup SQLite bestanden
                var dbFiles = Directory.GetFiles(dataPath, "*.db*");
                foreach (var dbFile in dbFiles)
                {
                    var fileName = Path.GetFileName(dbFile);
                    var destPath = Path.Combine(dbBackupPath, fileName);

                    // Voor SQLite WAL mode, probeer eerst te checkpoint
                    if (fileName.EndsWith(".db"))
                    {
                        await CheckpointDatabase(dbFile);
                    }

                    File.Copy(dbFile, destPath, true);
                }

                // Backup andere data bestanden
                var otherFiles = Directory.GetFiles(dataPath, "*")
                    .Where(f => !Path.GetFileName(f).StartsWith("jellyfin.db"));

                foreach (var file in otherFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var destPath = Path.Combine(dbBackupPath, fileName);
                    File.Copy(file, destPath, true);
                }
            }
        }

        private async Task BackupMetadata(string backupPath)
        {
            var metadataPath = Path.Combine(_jellyfinDataPath, "metadata");
            var metadataBackupPath = Path.Combine(backupPath, "metadata");

            if (Directory.Exists(metadataPath))
            {
                Directory.CreateDirectory(metadataBackupPath);

                // Selectief metadata backup (dit kan groot zijn!)
                if (_config.IncludeMetadata)
                {
                    await CopyDirectoryAsync(metadataPath, metadataBackupPath, new[]
                    {
                        "*.xml",
                        "*.json",
                        "*.nfo"
                    });
                }
                else
                {
                    // Alleen essentiële metadata
                    await CopyDirectoryAsync(metadataPath, metadataBackupPath, new[]
                    {
                        "*.xml",
                        "*.json"
                    }, maxDepth: 2);
                }
            }
        }

        private async Task BackupPlugins(string backupPath)
        {
            var pluginsPath = Path.Combine(_jellyfinDataPath, "plugins");
            var pluginsBackupPath = Path.Combine(backupPath, "plugins");

            if (Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsBackupPath);
                await CopyDirectoryAsync(pluginsPath, pluginsBackupPath);
            }
        }

        private async Task CopyDirectoryAsync(
            string sourceDir,
            string targetDir,
            string[]? patterns = null,  // Add ? to make it nullable
            int maxDepth = int.MaxValue,
            int currentDepth = 0)
        {
            if (currentDepth >= maxDepth) return;

            Directory.CreateDirectory(targetDir);

            // Copy files
            var files = patterns == null
                ? Directory.GetFiles(sourceDir)
                : patterns.SelectMany(pattern => Directory.GetFiles(sourceDir, pattern)).Distinct();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destDir = Path.Combine(targetDir, dirName);
                await CopyDirectoryAsync(dir, destDir, patterns, maxDepth, currentDepth + 1);
            }
        }

        private async Task CheckpointDatabase(string dbPath)
        {
            try
            {
                // SQLite checkpoint om WAL file te legen
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Kon database checkpoint niet uitvoeren voor {dbPath}");
            }
        }

        private async Task UploadToMinio(string filePath, string objectName)
        {
            await EnsureBucketExists();

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_config.BucketName)
                .WithObject($"backups/{objectName}")
                .WithFileName(filePath));

            // Cleanup oude backups indien geconfigureerd
            if (_config.RetentionDays > 0)
            {
                await CleanupOldBackups();
            }
        }

        private async Task EnsureBucketExists()
        {
            var bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_config.BucketName));

            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_config.BucketName));
            }
        }

        private string GetJellyfinDataPath()
        {
            // Probeer verschillende locaties
            var possiblePaths = new[]
            {
                Environment.GetEnvironmentVariable("JELLYFIN_DATA_DIR"),
                "/config", // Docker
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jellyfin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jellyfin"),
                "/var/lib/jellyfin" // Linux systeem installatie
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }
            }

            throw new DirectoryNotFoundException("Kan Jellyfin data directory niet vinden");
        }

        private async Task CleanupOldBackups()
        {
            try
            {
                // Skip cleanup for now - we'll implement this later when we know MinIO is working
                _logger.LogInformation("Backup cleanup overgeslagen voor nu");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kon oude backups niet opruimen");
            }
        }
    }
}