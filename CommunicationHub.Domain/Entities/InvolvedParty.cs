using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class InvolvedParty
{
    public int PartyId { get; set; }

    public int? ClaimId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? InvolvedPartyType { get; set; }

    public bool? IsActive { get; set; }

    public virtual Claim? Claim { get; set; }

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();
}
