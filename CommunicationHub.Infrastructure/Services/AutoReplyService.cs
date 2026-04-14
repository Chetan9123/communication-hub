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

            if (claim == null || claim.ClaimAdjuster?.Adjuster == null) return;

            var adjuster = claim.ClaimAdjuster.Adjuster;
            var party = claim.InvolvedParties.FirstOrDefault(p => p.PartyId == partyId);

            // Cooldown logic: Send only if adjuster is inactive and no auto-reply sent in the last 6 hours
            // Normalized check for IsActive being explicitly false (Out of Office)
            if (adjuster.IsActive == false && (!claim.LastAutoReplySent.HasValue || (DateTime.UtcNow - claim.LastAutoReplySent.Value).TotalHours >= 6))
            {
                _logger.LogInformation("[AutoReply] Triggered for ClaimId {ClaimId}, PartyId {PartyId} via {Channel}", claimId, partyId, channelName);

                var messageBody = "The assigned adjuster is currently unavailable (Out of Office). We will get back to you shortly.";

                bool success = false;
                if (channelName == "Email" && !string.IsNullOrEmpty(party?.Email))
                {
                    var result = await _emailService.SendEmailAsync(claimId, partyId, party.Email, $"Re: Claim #{claim.ClaimNumber} - Automated Reply", messageBody, adjuster.AdjusterId);
                    success = result.Sent;
                }
                else if (channelName == "SMS" && !string.IsNullOrEmpty(party?.Phone))
                {
                    success = await _smsService.SendSmsAsync(party.Phone, messageBody);
                }
                else if (channelName == "WhatsApp" && !string.IsNullOrEmpty(party?.Phone))
                {
                    var result = await _whatsAppService.SendWhatsAppAsync(party.Phone, messageBody, null, null);
                    success = result.Success;
                }

                if (success)
                {
                    claim.LastAutoReplySent = DateTime.UtcNow;
                    _context.Claims.Update(claim);
                    await _context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AutoReply] Failed to trigger auto-reply for ClaimId {ClaimId}", claimId);
        }
    }
}
