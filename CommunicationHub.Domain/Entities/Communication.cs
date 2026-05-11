using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class Communication
{
    public Guid CommunicationId { get; set; }
    
    public string? Sid { get; set; }

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

    public bool? ReadAt { get; set; }

    public DateTime? ReadAtDate { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
    
    public string? Notes { get; set; }


    public virtual Adjuster? Adjuster { get; set; }

    public virtual Channel? Channel { get; set; }

    public virtual Claim? Claim { get; set; }

    public virtual ICollection<MessageAttachment> MessageAttachments { get; set; } = new List<MessageAttachment>();

    public virtual InvolvedParty? Party { get; set; }
}
