using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Jellyfin.Plugin.MinioBackup.Configuration;

namespace Jellyfin.Plugin.MinioBackup.Api
{
    /// <summary>
    /// API controller for MinIO Backup Plugin.
    /// </summary>
    [ApiController]
    [Route("MinioBackup")]
    public class MinioBackupController : ControllerBase
    {
        private readonly ILogger<MinioBackupController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MinioBackupController"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{MinioBackupController}"/> interface.</param>
        public MinioBackupController(ILogger<MinioBackupController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tests MinIO connection.
        /// </summary>
        /// <param name="config">MinIO configuration to test.</param>
        /// <returns>Test result.</returns>
        [HttpPost("TestConnection")]
        public async Task<ActionResult<TestConnectionResult>> TestConnection([FromBody] TestConnectionRequest config)
        {
            try
            {
                var minioClient = new MinioClient()
                    .WithEndpoint(config.MinioEndpoint)
                    .WithCredentials(config.AccessKey, config.SecretKey)
                    .WithSSL(config.UseSSL)
                    .Build();

                // Test connection by checking if bucket exists or can be created
                var bucketExists = await minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(config.BucketName));

                return new TestConnectionResult 
                { 
                    Success = true, 
                    Message = bucketExists ? "Connection successful! Bucket exists." : "Connection successful! Bucket will be created when needed."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO connection test failed");
                return new TestConnectionResult 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }
    }

    /// <summary>
    /// Test connection request model.
    /// </summary>
    public class TestConnectionRequest
    {
        /// <summary>
        /// Gets or sets the MinIO endpoint.
        /// </summary>
        public string MinioEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access key.
        /// </summary>
        public string AccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the secret key.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the bucket name.
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL.
        /// </summary>
        public bool UseSSL { get; set; }
    }

    /// <summary>
    /// Test connection result model.
    /// </summary>
    public class TestConnectionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the test was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the success message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error { get; set; } = string.Empty;
    }
}