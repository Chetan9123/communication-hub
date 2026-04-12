using System;
using System.IO;
using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IS3Service
{
    /// <summary>
    /// Uploads a file to S3 with a structured path: attachments/{year}/{month}/{communicationId}/{fileName}
    /// </summary>
    Task<string> UploadFileAsync(Stream file, string fileName, string contentType, Guid communicationId);

    /// <summary>
    /// Generates a time-limited pre-signed URL for a private S3 object.
    /// </summary>
    Task<string> GeneratePreSignedUrlAsync(string s3Key, int expiryMinutes = 60);

    /// <summary>
    /// Downloads a file from S3 as a byte array.
    /// </summary>
    Task<byte[]> DownloadFileAsync(string s3Key);

    /// <summary>
    /// Downloads a file from S3 and returns a readable Stream.
    /// The caller is responsible for disposing the Stream.
    /// </summary>
    Task<Stream> GetFileStreamAsync(string s3Key);
}
