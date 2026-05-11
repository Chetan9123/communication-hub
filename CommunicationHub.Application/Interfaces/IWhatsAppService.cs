using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IWhatsAppService
{
    /// <summary>
    /// Sends a WhatsApp message via Twilio.
    /// Supports text and optional media attachments.
    /// </summary>
    /// <param name="to">Recipient phone number (e.g. +1234567890)</param>
    /// <param name="message">Text body of the message</param>
    /// <param name="mediaUrls">Optional list of media URLs to attach</param>
    /// <param name="statusCallback">Optional URL for Twilio to send status updates to</param>
    /// <returns>A WhatsAppSendResult containing success status, SID, and any error details.</returns>
    Task<WhatsAppSendResult> SendWhatsAppAsync(string to, string message, IEnumerable<string>? mediaUrls = null, string? statusCallback = null);
}
