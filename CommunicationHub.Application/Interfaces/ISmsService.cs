using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

/// <summary>
/// Technical wrapper for sending SMS messages via a provider (e.g. Twilio).
/// Keeps business logic like ClaimId/PartyId out of the Infrastructure service.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a plain text SMS to the specified recipient.
    /// </summary>
    /// <param name="to">Recipient phone number in international format (e.g. +91XXXXXXXXXX)</param>
    /// <param name="message">The SMS message body</param>
    /// <returns>True if the message was successfully accepted by the provider.</returns>
    Task<bool> SendSmsAsync(string to, string message);
}
