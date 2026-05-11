using System;

namespace CommunicationHub.API.DTOs;

public class CommunicationDto
{
    public Guid CommunicationId { get; set; }
    public int? ClaimId { get; set; }
    public int? PartyId { get; set; }
    public int? ChannelId { get; set; }
    public int? AdjusterId { get; set; }
    public string? Direction { get; set; }
    public string? MessageBody { get; set; }
    public string? MessageType { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? ReadAtDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}
