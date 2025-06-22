using System;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace Jellyfin.Plugin.MinioBackup.Api
{
    /// <summary>
    /// MinIO Backup API controller.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "RequiresElevation")]
    [Route("MinioBackup")]
    [Produces(MediaTypeNames.Application.Json)]
    public class MinioBackupApiController : ControllerBase
    {
        private readonly ILogger<MinioBackupApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MinioBackupApiController"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{MinioBackupApiController}"/> interface.</param>
        public MinioBackupApiController(ILogger<MinioBackupApiController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tests the MinIO connection.
        /// </summary>
        /// <param name="request">The connection test request.</param>
        /// <returns>The test result.</returns>
        [HttpPost("TestConnection")]
        public async Task<ActionResult<TestConnectionResponse>> TestConnection([FromBody] TestConnectionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MinioEndpoint))
                {
                    return new TestConnectionResponse { Success = false, Error = "MinIO endpoint is required" };
                }

                var minioClient = new MinioClient()
                    .WithEndpoint(request.MinioEndpoint)
                    .WithCredentials(request.AccessKey, request.SecretKey)
                    .WithSSL(request.UseSSL)
                    .Build();

                // Test connection by checking if bucket exists
                var bucketExists = await minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(request.BucketName));

                var message = bucketExists 
                    ? "Connection successful! Bucket exists." 
                    : "Connection successful! Bucket will be created when needed.";

                return new TestConnectionResponse { Success = true, Message = message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MinIO connection test failed");
                return new TestConnectionResponse { Success = false, Error = ex.Message };
            }
        }
    }

    /// <summary>
    /// Test connection request.
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
    /// Test connection response.
    /// </summary>
    public class TestConnectionResponse
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