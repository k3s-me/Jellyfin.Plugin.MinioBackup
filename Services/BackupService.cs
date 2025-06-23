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
using System.Net.Http;

namespace Jellyfin.Plugin.MinioBackup.Services
{
    /// <summary>
    /// Service for creating and managing Jellyfin backups to MinIO storage.
    /// </summary>
    public class BackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly PluginConfiguration _config;
        private readonly IMinioClient? _minioClient;
        private readonly string _jellyfinDataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupService"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BackupService}"/> interface.</param>
        /// <param name="config">Plugin configuration.</param>
        public BackupService(ILogger<BackupService> logger, PluginConfiguration config)
        {
            _logger = logger;
            _config = config;

            // Debug logging
            _logger.LogInformation("BackupService initialized with config:");
            _logger.LogInformation("MinIO Endpoint: {Endpoint}", config?.MinioEndpoint ?? "[NULL]");
            _logger.LogInformation("Bucket Name: {BucketName}", config?.BucketName ?? "[NULL]");
            _logger.LogInformation("Access Key: {AccessKey}", config?.AccessKey ?? "[NULL]");
            _logger.LogInformation("Region: {Region}", config?.Region ?? "[NULL]");
            _logger.LogInformation("Use SSL: {UseSSL}", config?.UseSSL ?? false);

            _jellyfinDataPath = GetJellyfinDataPath();

            if (!string.IsNullOrEmpty(config?.MinioEndpoint))
            {
                // Create custom HttpClientHandler to bypass SSL validation
                var httpClientHandler = new HttpClientHandler()
                {
                    // This bypasses SSL certificate validation - use only for internal services
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    {
                        _logger.LogInformation("SSL Certificate validation bypassed for MinIO connection");
                        return true; // Accept all certificates
                    }
                };

                var httpClient = new HttpClient(httpClientHandler);

                _minioClient = new MinioClient()
                    .WithEndpoint(config.MinioEndpoint)
                    .WithCredentials(config.AccessKey, config.SecretKey)
                    .WithSSL(config.UseSSL)
                    .WithHttpClient(httpClient)
                    .WithRegion(config.Region)
                    .Build();

                _logger.LogInformation("MinIO client initialized with custom SSL validation bypass");
            }
            else
            {
                _logger.LogWarning("MinIO endpoint not configured, MinioClient not initialized");
            }
        }

        /// <summary>
        /// Creates a full backup of Jellyfin data and uploads it to MinIO.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateFullBackup()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var tempBackupPath = Path.Combine(Path.GetTempPath(), $"jellyfin_backup_{timestamp}");

            try
            {
                Directory.CreateDirectory(tempBackupPath);

                // Conditional backups based on config
                if (_config.BackupConfig)
                    await BackupConfigurations(tempBackupPath);
            
                if (_config.BackupPlugins)
                    await BackupPlugins(tempBackupPath);
            
                if (_config.BackupData)
                    await BackupDatabase(tempBackupPath);
            
                if (_config.BackupLog)
                    await BackupLogs(tempBackupPath);
            
                if (_config.BackupMetadata)
                    await BackupMetadata(tempBackupPath);
            
                if (_config.BackupRoot)
                    await BackupRoot(tempBackupPath);

                // Compress and upload
                var zipPath = $"{tempBackupPath}.zip";
                ZipFile.CreateFromDirectory(tempBackupPath, zipPath);
                await UploadToMinio(zipPath, $"full_backup_{timestamp}.zip");

                _logger.LogInformation($"Full backup completed: {zipPath}");
            }
            finally
            {
                if (Directory.Exists(tempBackupPath))
                    Directory.Delete(tempBackupPath, true);
            }
        }
        
        private async Task BackupLogs(string backupPath)
        {
            var logsPath = Path.Combine(_jellyfinDataPath, "log");
            var logsBackupPath = Path.Combine(backupPath, "log");

            if (Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsBackupPath);
                await CopyDirectoryAsync(logsPath, logsBackupPath, new[] { "*.log", "*.txt" });
            }
        }

        private async Task BackupRoot(string backupPath)
        {
            var rootBackupPath = Path.Combine(backupPath, "root");
            var rootPath = Path.Combine(_jellyfinDataPath, "root");

            if (Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootBackupPath);
                await CopyDirectoryAsync(rootPath, rootBackupPath);
            }
        }
        
        private async Task BackupConfigurations(string backupPath)
        {
            var configBackupPath = Path.Combine(backupPath, "config");
            Directory.CreateDirectory(configBackupPath);

            var configPath = Path.Combine(_jellyfinDataPath, "config");

            if (Directory.Exists(configPath))
            {
                await CopyDirectoryAsync(configPath, configBackupPath);
            }
        }

        private async Task BackupDatabase(string backupPath)
        {
            var dbBackupPath = Path.Combine(backupPath, "data");
            Directory.CreateDirectory(dbBackupPath);

            var dataPath = Path.Combine(_jellyfinDataPath, "data");

            if (Directory.Exists(dataPath))
            {
                // Backup SQLite files
                var dbFiles = Directory.GetFiles(dataPath, "*.db*");
                foreach (var dbFile in dbFiles)
                {
                    var fileName = Path.GetFileName(dbFile);
                    var destPath = Path.Combine(dbBackupPath, fileName);

                    // For SQLite WAL mode, try to checkpoint first
                    if (fileName.EndsWith(".db"))
                    {
                        await CheckpointDatabase(dbFile);
                    }

                    File.Copy(dbFile, destPath, true);
                }

                // Backup other data files
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
                
                await CopyDirectoryAsync(metadataPath, metadataBackupPath);
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
            string[]? patterns = null,
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
                using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not perform database checkpoint for {dbPath}");
            }
        }

        private async Task UploadToMinio(string filePath, string objectName)
        {
            if (_minioClient == null)
            {
                throw new InvalidOperationException("MinIO client not initialized");
            }

            try 
            {
                _logger.LogInformation("Testing MinIO connection first...");
                _logger.LogInformation("MinIO Config - Endpoint: {Endpoint}, SSL: {SSL}, Bucket: {Bucket}", 
                    _config.MinioEndpoint, _config.UseSSL, _config.BucketName);
                
                // Test basic connectivity first
                var bucketExists = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_config.BucketName));

                _logger.LogInformation("Bucket '{BucketName}' exists: {Exists}", _config.BucketName, bucketExists);

                if (!bucketExists)
                {
                    _logger.LogInformation("Creating bucket '{BucketName}'...", _config.BucketName);
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_config.BucketName));
                    _logger.LogInformation("Bucket created successfully");
                }

                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("File info - Size: {Size} bytes, Exists: {Exists}", 
                    fileInfo.Length, fileInfo.Exists);

                _logger.LogInformation("Starting upload of file '{FilePath}' as '{ObjectName}'...", filePath, objectName);
                
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_config.BucketName)
                    .WithObject($"backups/{objectName}")
                    .WithFileName(filePath)
                    .WithContentType("application/zip");

                await _minioClient.PutObjectAsync(putObjectArgs);
                
                _logger.LogInformation("Upload completed successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("XML document"))
            {
                _logger.LogError(ex, "XML parsing error occurred. This usually means MinIO returned an error response instead of expected XML.");
                
                await LogMinioErrorResponse();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO operation failed. Endpoint: {Endpoint}, Bucket: {Bucket}, SSL: {SSL}", 
                    _config.MinioEndpoint, _config.BucketName, _config.UseSSL);
                throw;
            }
        }
        
        private async Task LogMinioErrorResponse()
        {
            try
            {
                var protocol = _config.UseSSL ? "https" : "http";
                var url = $"{protocol}://{_config.MinioEndpoint}/{_config.BucketName}?uploads=";
                
                _logger.LogInformation("Trying to get MinIO multipart upload initiation response from: {Url}", url);
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("AWS4-HMAC-SHA256", 
                        "Credential=test"); // Simplified for testing
                
                var response = await httpClient.PostAsync(url, new StringContent(""));
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogError("MinIO Multipart Upload Error Response: Status={StatusCode}, Content={Content}", 
                    response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve MinIO error response details");
            }
        }

        private async Task EnsureBucketExists()
        {
            if (_minioClient == null)
            {
                throw new InvalidOperationException("MinIO client not initialized");
            }
    
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
            var possiblePaths = new string?[]
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

            throw new DirectoryNotFoundException("Cannot find Jellyfin data directory");
        }
    }
}