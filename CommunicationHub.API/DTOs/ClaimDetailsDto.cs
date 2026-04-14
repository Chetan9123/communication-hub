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

public class InvolvedPartyDto
{
    public int PartyId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName => $"{FirstName} {LastName}".Trim();
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? InvolvedPartyType { get; set; }
    public bool? IsActive { get; set; }
}
