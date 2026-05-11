using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an outbound email via SMTP (MailKit) and persists it as a Communication record.
    /// Returns true only if the SMTP server accepted the email successfully.
    /// </summary>
    Task<(bool Sent, Guid CommunicationId)> SendEmailAsync(
        int claimId,
        int partyId,
        string to,
        string subject,
        string body,
        int adjusterId,
        IEnumerable<(string FileName, Stream Data, string ContentType)>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an inbound email from an inbound webhook.
    /// Extracts ClaimId from the subject and persists a Communication record.
    /// </summary>
    Task<bool> ProcessInboundEmailAsync(
        string from,
        string subject,
        string text,
        string? html,
        IEnumerable<(string FileName, Stream Data, string ContentType)>? attachments = null,
        CancellationToken cancellationToken = default);
}
