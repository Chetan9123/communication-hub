using System;

namespace CommunicationHub.API.DTOs;

public class AttachmentUploadResponse
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; }
    public string S3Key { get; set; }
}
