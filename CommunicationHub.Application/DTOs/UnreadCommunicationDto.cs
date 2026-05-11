using System;

namespace CommunicationHub.API.DTOs;

/// <summary>
/// DTO for the Communication Hub To-Do/Unread view
/// </summary>
public class UnreadCommunicationDto
{
    public Guid CommunicationId { get; set; }
    public int ClaimId { get; set; }
    public string? ClaimNumber { get; set; }
    public string? PolicyNumber { get; set; }
    public int PartyId { get; set; }
    public string? SenderName { get; set; }
    public string? CommunicationMode { get; set; }
    public string? MessagePreview { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public bool? IsRead { get; set; }
    public string? Status { get; set; }
    public string? SenderPhone { get; set; }
    public string? SenderEmail { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}
