using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class Adjuster
{
    public int AdjusterId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<ClaimAdjuster> ClaimAdjusters { get; set; } = new List<ClaimAdjuster>();

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();
}
