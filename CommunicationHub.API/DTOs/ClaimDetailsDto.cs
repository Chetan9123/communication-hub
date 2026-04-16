using System;
using System.Collections.Generic;

namespace CommunicationHub.API.DTOs;

public class ClaimDetailsDto
{
    public int ClaimId { get; set; }
    public string? ClaimNumber { get; set; }
    public string? PolicyNumber { get; set; }
    public DateTime? ClaimFiledOn { get; set; }
    public DateTime? ClaimClosedOn { get; set; }
    public string? Status { get; set; }
    public string? AssignedAdjusterName { get; set; }
    public bool? IsAdjusterActive { get; set; }
    public List<InvolvedPartyDto>? InvolvedParties { get; set; }
}
