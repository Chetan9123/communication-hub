using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class Channel
{
    public int ChannelId { get; set; }

    public string? Name { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();
}
