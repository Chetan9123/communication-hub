using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class Claim
{
    public int ClaimId { get; set; }

    public string? PolicyNumber { get; set; }

    public string? ClaimNumber { get; set; }

    public DateTime? ClaimFiledOn { get; set; }

    public DateTime? ClaimClosedOn { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ClaimAdjuster? ClaimAdjuster { get; set; }

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

    public virtual ICollection<InvolvedParty> InvolvedParties { get; set; } = new List<InvolvedParty>();
}
