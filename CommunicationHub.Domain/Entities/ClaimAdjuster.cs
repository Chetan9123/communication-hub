using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class ClaimAdjuster
{
    public int ClaimAdjusterId { get; set; }

    public int? ClaimId { get; set; }

    public int? AdjusterId { get; set; }

    public bool? IsPrimary { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? UnassignedAt { get; set; }

    public virtual Adjuster? Adjuster { get; set; }

    public virtual Claim? Claim { get; set; }
}
