using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CommunicationHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.Infrastructure.Services;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3Service> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"] ?? throw new ArgumentNullException("AWS:BucketName is not configured");
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream file, string fileName, string contentType, Guid communicationId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var year = now.Year.ToString();
            var month = now.Month.ToString("D2");
            
            // Generate a unique file name to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            
            // Structured path: attachments/{year}/{month}/{communicationId}/{fileName}
            var s3Key = $"attachments/{year}/{month}/{communicationId}/{uniqueFileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = file,
                ContentType = contentType,
                AutoCloseStream = true
            };

            await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Successfully uploaded file to S3: {S3Key}", s3Key);
            
            return s3Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> GeneratePreSignedUrlAsync(string s3Key, int expiryMinutes = 60)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-signed URL for key: {S3Key}", s3Key);
            throw;
        }
    }

    public async Task<byte[]> DownloadFileAsync(string s3Key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            using var response = await _s3Client.GetObjectAsync(request);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from S3: {S3Key}", s3Key);
            throw;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string s3Key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            var response = await _s3Client.GetObjectAsync(request);
            // Returns a Stream that the caller MUST dispose.
            return response.ResponseStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file stream from S3: {S3Key}", s3Key);
            throw;
        }
    }
}
