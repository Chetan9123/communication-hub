using System;
using System.Collections.Generic;

namespace CommunicationHub.API.DTOs;

public class AdjusterDashboardDto
{
    public int AdjusterId { get; set; }
    public string? AdjusterName { get; set; }
    public string? Email { get; set; }
    public int UnreadCommunicationCount { get; set; }
    public List<AssignedClaimDto>? AssignedClaims { get; set; }
}

public class AssignedClaimDto
{
    public int ClaimId { get; set; }
    public string? ClaimNumber { get; set; }
    public string? PolicyNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? ClaimFiledOn { get; set; }
    public int UnreadCommunicationCount { get; set; }
}
