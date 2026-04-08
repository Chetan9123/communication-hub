using System.Threading;
using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an outbound email via SendGrid and persists it as a Communication record.
    /// Returns true only if SendGrid accepted the email (2xx response).
    /// </summary>
    Task<(bool Sent, Guid CommunicationId)> SendEmailAsync(
        int claimId,
        int partyId,
        string to,
        string subject,
        string body,
        int adjusterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an inbound email from the SendGrid Inbound Parse webhook.
    /// Extracts ClaimId from the subject and persists a Communication record.
    /// </summary>
    Task<bool> ProcessInboundEmailAsync(
        string from,
        string subject,
        string text,
        string? html,
        CancellationToken cancellationToken = default);
}
