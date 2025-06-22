using System;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
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

        public MinioBackupApiController(ILogger<MinioBackupApiController> logger)
        {
            _logger = logger;
        }

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

    public class TestConnectionRequest
    {
        public string MinioEndpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public bool UseSSL { get; set; }
    }

    public class TestConnectionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}