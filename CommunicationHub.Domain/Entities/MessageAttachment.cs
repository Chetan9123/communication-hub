using System;
using System.Collections.Generic;

namespace CommunicationHub.Domain.Entities;

public partial class MessageAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid? CommunicationId { get; set; }

    public string? FileUrl { get; set; }

    public string? MimeType { get; set; }

    public string? FileType { get; set; }

    public int? FileSize { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Communication? Communication { get; set; }
}
