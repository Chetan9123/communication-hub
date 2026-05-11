using System;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.Infrastructure.Services;

public class AutoReplyService : IAutoReplyService
{
    private readonly CommunicationHubDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<AutoReplyService> _logger;

    public AutoReplyService(
        CommunicationHubDbContext context,
        IEmailService emailService,
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        ILogger<AutoReplyService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task TriggerAutoReplyIfInactiveAsync(int claimId, int partyId, string channelName)
    {
        try
        {
            // Fetch claim with assigned adjuster and the contacting party
            var claim = await _context.Claims
                .Include(c => c.ClaimAdjuster)
                .ThenInclude(ca => ca!.Adjuster)
                .Include(c => c.InvolvedParties)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
            {
                _logger.LogWarning("[AutoReply] Aborting: Claim {ClaimId} not found.", claimId);
                return;
            }

            if (claim.ClaimAdjuster?.Adjuster == null)
            {
                _logger.LogWarning("[AutoReply] Aborting: No adjuster assigned to Claim {ClaimId}.", claimId);
                return;
            } 

            var adjuster = claim.ClaimAdjuster.Adjuster;
            var party = claim.InvolvedParties.FirstOrDefault(p => p.PartyId == partyId);

            if (party == null)
            {
                _logger.LogWarning("[AutoReply] Aborting: Party {PartyId} not found in Claim {ClaimId}.", partyId, claimId);
                return;
            }

            // check if adjuster is active
            if (adjuster.IsActive != false)
            {
                _logger.LogInformation("[AutoReply] Skipping: Adjuster {AdjusterId} is currently Active.", adjuster.AdjusterId);
                return;
            }

            // Cooldown logic: Send only if no auto-reply sent in the last 1 hour
            if (claim.LastAutoReplySent.HasValue && (DateTime.UtcNow - claim.LastAutoReplySent.Value).TotalHours < 1)
            {
                _logger.LogInformation("[AutoReply] Skipping: Cooldown active. Last reply sent at {Time}.", claim.LastAutoReplySent.Value);
                return;
            }

            _logger.LogInformation("[AutoReply] Triggering for Claim {ClaimId}, Party {PartyId} via {Channel}", claimId, partyId, channelName);

            var messageBody = "The assigned adjuster is currently unavailable (Out of Office). We will get back to you shortly.";
            bool success = false;

            if (string.Equals(channelName, "Email", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(party.Email))
                {
                    var result = await _emailService.SendEmailAsync(claimId, partyId, party.Email, $"Re: Claim #{claim.ClaimNumber} - Automated Reply", messageBody, adjuster.AdjusterId);
                    success = result.Sent;
                }
                else
                {
                    _logger.LogWarning("[AutoReply] Cannot send Email: Party {PartyId} has no email address.", partyId);
                }
            }
            else if (string.Equals(channelName, "SMS", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(party.Phone))
                {
                    success = await _smsService.SendSmsAsync(party.Phone, messageBody);
                }
                else
                {
                    _logger.LogWarning("[AutoReply] Cannot send SMS: Party {PartyId} has no phone number.", partyId);
                }
            }
            else if (string.Equals(channelName, "WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(party.Phone))
                {
                    var result = await _whatsAppService.SendWhatsAppAsync(party.Phone, messageBody, null, null);
                    success = result.Success;
                }
                else
                {
                    _logger.LogWarning("[AutoReply] Cannot send WhatsApp: Party {PartyId} has no phone number.", partyId);
                }
            }
            else
            {
                _logger.LogWarning("[AutoReply] Unsupported channel: {Channel}", channelName);
            }

            if (success)
            {
                claim.LastAutoReplySent = DateTime.UtcNow;
                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[AutoReply] Successfully sent and timestamp updated for Claim {ClaimId}.", claimId);
            }
            else
            {
                _logger.LogError("[AutoReply] Technical failure while sending message via {Channel}.", channelName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoReply] Critical exception for Claim {ClaimId}", claimId);
        }
    }
}
