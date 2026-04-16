using System;
using System.Collections.Generic;

namespace CommunicationHub.API.DTOs;

public class CommunicationThreadDto
{
    public int ClaimId { get; set; }
    public string? ClaimNumber { get; set; }
    public string? PolicyNumber { get; set; }
    public int PartyId { get; set; }
    public string? PartyName { get; set; }
    public List<CommunicationMessageDto>? Messages { get; set; }
}

public class CommunicationMessageDto
{
    public Guid CommunicationId { get; set; }
    public string? Direction { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Mode { get; set; }
    public string? MessageBody { get; set; }
    public string? Status { get; set; }
    public bool? IsRead { get; set; }
    public string? Notes { get; set; }
    public string? PartyName { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}

public class AttachmentDto
{
    public Guid AttachmentId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? MimeType { get; set; }
    public int? FileSize { get; set; }
}
