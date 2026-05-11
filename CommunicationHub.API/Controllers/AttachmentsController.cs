using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Domain.Entities;
using CommunicationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CommunicationHub.API.DTOs;

namespace CommunicationHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Ensure only authenticated users can access attachments
public class AttachmentsController : ControllerBase
{
    private readonly CommunicationHubDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly ILogger<AttachmentsController> _logger;

    public AttachmentsController(
        CommunicationHubDbContext context,
        IS3Service s3Service,
        ILogger<AttachmentsController> logger)
    {
        _context = context;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Generates a time-limited pre-signed URL for an attachment stored in S3.
    /// Default expiry is 60 minutes.
    /// </summary>
    [HttpGet("{attachmentId}/url")]
    public async Task<IActionResult> GetAttachmentUrl(Guid attachmentId, [FromQuery] int expiryMinutes = 60)
    {
        try
        {
            var attachment = await _context.MessageAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

            if (attachment == null)
            {
                return NotFound("Attachment not found.");
            }

            if (string.IsNullOrEmpty(attachment.S3Key))
            {
                // Fallback to FileUrl if S3Key is missing (for legacy or local files)
                if (!string.IsNullOrEmpty(attachment.FileUrl))
                {
                    return Ok(new { url = attachment.FileUrl, isPreSigned = false });
                }
                return BadRequest("Attachment is missing storage information.");
            }

            // Generate the pre-signed URL for private S3 object
            var preSignedUrl = await _s3Service.GeneratePreSignedUrlAsync(attachment.S3Key, expiryMinutes);

            return Ok(new { url = preSignedUrl, isPreSigned = true, expiryMinutes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating URL for attachment {AttachmentId}", attachmentId);
            return StatusCode(500, "Internal server error while generating attachment URL.");
        }
    }

    /// <summary>
    /// GET /api/attachments/{attachmentId}/download
    /// Streams the file from S3 through the backend to the client.
    /// This avoids CORS issues that occur when the browser directly fetches S3 URLs.
    /// </summary>
    [HttpGet("{attachmentId}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
    {
        try
        {
            var attachment = await _context.MessageAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);

            if (attachment == null)
                return NotFound("Attachment not found.");

            string downloadUrl;

            if (!string.IsNullOrEmpty(attachment.S3Key))
            {
                // Generate a fresh pre-signed URL (1 hour)
                downloadUrl = await _s3Service.GeneratePreSignedUrlAsync(attachment.S3Key, 60);

                var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync(downloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    client.Dispose();
                    return StatusCode((int)response.StatusCode, "Failed to retrieve file from storage.");
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, attachment.MimeType ?? "application/octet-stream", attachment.FileName ?? $"attachment_{attachmentId}");
            }
            else if (!string.IsNullOrEmpty(attachment.FileUrl))
            {
                // Simple stream proxy for external URLs (not protected but for stability)
                var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync(attachment.FileUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    client.Dispose();
                    return StatusCode((int)response.StatusCode, "Failed to retrieve external file.");
                }
                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, attachment.MimeType ?? "application/octet-stream", attachment.FileName ?? $"attachment_{attachmentId}");
            }

            return BadRequest("Attachment is missing storage information.");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming download for attachment {AttachmentId}", attachmentId);
            return StatusCode(500, "Internal server error while downloading attachment.");
        }
    }


    /// <summary>
    /// POST /api/attachments/upload
    /// Uploads a file to S3 and returns the metadata.
    /// This is used for attaching files to outgoing communications.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(AttachmentUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttachmentUploadResponse>> UploadAttachment([FromForm] IFormFile file)
    {
        _logger.LogInformation("[AttachmentsController] Upload request received for file: {FileName}, ContentType: {ContentType}, Size: {Size} bytes", 
            file?.FileName, file?.ContentType, file?.Length);

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // 1. Validation (Size: 25MB)
        if (file.Length > 25 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 25MB limit.");
        }

        // 2. Validation (Type)
        var contentType = file.ContentType;
        var allowedTypes = new[] { 
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
            "audio/mpeg", "audio/wav", "audio/ogg", "audio/aac",
            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/zip", "application/x-zip-compressed", "text/plain"
        };

        if (!allowedTypes.Contains(contentType) && !contentType.StartsWith("image/") && !contentType.StartsWith("video/"))
        {
            // Note: We still allow common video/image types via startsWith if not explicitly in the list
            _logger.LogInformation("Uploading via startsWith match: {Type}", contentType);
        }
        else if (!allowedTypes.Contains(contentType))
        {
            return BadRequest($"Unsupported file type: {contentType}");
        }

        try
        {
            // Upload to S3 with a temporary GUID since we don't have a CommunicationId yet
            var tempCommId = Guid.Empty; 
            using var stream = file.OpenReadStream();
            
            var s3Key = await _s3Service.UploadFileAsync(stream, file.FileName, contentType, tempCommId);

            // Save Metadata
            var attachment = new MessageAttachment
            {
                AttachmentId = Guid.NewGuid(),
                FileName = file.FileName,
                S3Key = s3Key,
                MimeType = contentType,
                FileType = Path.GetExtension(file.FileName).TrimStart('.'),
                FileSize = (int)file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded and tracked attachment: {AttachmentId}", attachment.AttachmentId);

            return Ok(new AttachmentUploadResponse
            { 
                AttachmentId = attachment.AttachmentId, 
                FileName = attachment.FileName,
                S3Key = attachment.S3Key
            });
        }
        catch (Amazon.S3.AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "AmazonS3Exception: Error uploading manual attachment: {FileName}. Message: {Message}. Status Code: {StatusCode}.", file.FileName, s3Ex.Message, s3Ex.StatusCode);
            return StatusCode(500, $"Internal server error during upload: {s3Ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading manual attachment: {FileName}", file.FileName);
            return StatusCode(500, "Internal server error during upload.");
        }
    }
}
