using System;
using System.Collections.Generic;

namespace CommunicationHub.API.DTOs;

public class SendCommunicationRequest
{
    public int ClaimId { get; set; }
    public int PartyId { get; set; }
    public string? Mode { get; set; } // Email, SMS, WhatsApp
    public string? To { get; set; }
    public string? Cc { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Signature { get; set; }
    public List<string>? AttachmentUrls { get; set; }
}
