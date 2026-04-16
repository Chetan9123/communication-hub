using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;

namespace CommunicationHub.Application.Interfaces;

public interface ICommunicationService
{
    /// <summary>
    /// Gets all unread communications for the logged-in adjuster
    /// </summary>
    Task<List<UnreadCommunicationDto>> GetUnreadCommunicationsAsync(int adjusterId);

    /// <summary>
    /// Updates the read status of a communication
    /// </summary>
    Task<bool> UpdateReadStatusAsync(Guid communicationId, bool isRead);

    /// <summary>
    /// Gets the communication thread for a specific claim and party
    /// </summary>
    Task<CommunicationThreadDto> GetCommunicationThreadAsync(int claimId, int partyId);

    /// <summary>
    /// Gets all communications across all involved parties for a specific claim
    /// </summary>
    Task<CommunicationThreadDto> GetClaimCommunicationThreadAsync(int claimId);

    /// <summary>
    /// Updates the notes for a specific communication
    /// </summary>
    Task<bool> UpdateNotesAsync(Guid communicationId, string notes);

    /// <summary>
    /// Sends a new communication
    /// </summary>
    Task<(Guid CommunicationId, string? WarningMessage)> SendCommunicationAsync(SendCommunicationRequest request, int adjusterId);

    /// <summary>
    /// Validates if the adjuster is assigned to the claim and if the communication channel is enabled
    /// </summary>
    Task<bool> ValidateAdjusterAccessAsync(int adjusterId, int claimId);

    /// <summary>
    /// Gets the communication channel configuration
    /// </summary>
    Task<Dictionary<string, bool>> GetEnabledChannelsAsync();

    /// <summary>
    /// Processes an incoming SMS from Twilio.
    /// Matches the sender to an InvolvedParty/Claim or logs as unmatched.
    /// </summary>
    Task<bool> ProcessIncomingSmsAsync(string fromNumber, string body, string messageSid);

    /// <summary>
    /// Processes an incoming WhatsApp message from Twilio.
    /// Includes media handling and SignalR notification.
    /// </summary>
    Task<bool> ProcessIncomingWhatsAppAsync(string fromNumber, string body, string messageSid, List<string>? mediaUrls = null);

    /// <summary>
    /// Updates the status of a communication based on a technical SID (e.g. Twilio SID).
    /// </summary>
    Task<bool> UpdateCommunicationStatusBySidAsync(string sid, string status);

    /// <summary>
    /// Syncs missing WhatsApp messages from Twilio in the last 24 hours.
    /// </summary>
    Task<int> SyncMissedTwilioMessagesAsync();
}
