using System;

namespace CommunicationHub.API.DTOs;

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
    public bool? IsInjured { get; set; }
}
