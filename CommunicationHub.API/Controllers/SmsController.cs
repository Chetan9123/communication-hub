using System;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.API.Security;
using CommunicationHub.Application.DTOs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using CommunicationHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly CommunicationHubDbContext _context;
    private readonly ILogger<SmsController> _logger;

    public SmsController(ISmsService smsService, CommunicationHubDbContext context, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/sms/send
    /// Sends an SMS via Twilio and logs it into the Communication history.
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // 1. Get current adjuster ID from token
            if (!User.TryGetAdjusterId(out var adjusterId))
                return Unauthorized(new { message = "Invalid or expired token." });

            // 2. Technical dispatch via TwilioSmsService
            // This is the "pure" technical service call the user requested.
            var isSent = await _smsService.SendSmsAsync(request.PhoneNumber, request.Message);

            // 3. Business Logging via DB Context
            // We link the technical event (SMS sent) to the Business context (Claim, Party).
            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                PartyId = request.PartyId,
                AdjusterId = adjusterId,
                ChannelId = 2, // SMS Channel ID
                Direction = "Outgoing",
                MessageBody = request.Message,
                MessageType = "SMS",
                Status = isSent ? "Sent" : "Failed",
                SentAt = isSent ? DateTime.UtcNow : null,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = true, // Outgoing messages are implicitly read by the sender
                ReadAtDate = DateTime.UtcNow
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync();

            if (!isSent)
            {
                _logger.LogWarning(
                    "[SMS] Status=FAILED | To={To} | ClaimId={ClaimId} | CommunicationId={Id}",
                    request.PhoneNumber, request.ClaimId, communication.CommunicationId);

                return BadRequest(new 
                { 
                    status = "Failed",
                    message = "SMS could not be delivered via Twilio. The attempt is recorded in the database.",
                    communicationId = communication.CommunicationId 
                });
            }

            _logger.LogInformation(
                "[SMS] Status=SENT | To={To} | ClaimId={ClaimId} | CommunicationId={Id}",
                request.PhoneNumber, request.ClaimId, communication.CommunicationId);

            return Ok(new 
            { 
                status = "Sent",
                message = "SMS sent and logged successfully.", 
                communicationId = communication.CommunicationId 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                status = "Error",
                message = "An internal error occurred while processing the SMS.", 
                error = ex.Message 
            });
        }
    }
}
