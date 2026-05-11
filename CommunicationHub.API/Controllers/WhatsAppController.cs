using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CommunicationHub.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(ICommunicationService communicationService, ILogger<WhatsAppController> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendWhatsApp([FromBody] SendCommunicationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.To) || string.IsNullOrEmpty(request.Body))
            {
                return BadRequest("Recipient and message body are required.");
            }

            // Ensure mode is set to WhatsApp
            request.Mode = "WhatsApp";

            // Get AdjusterId from claims
            var adjusterIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adjusterIdClaim == null || !int.TryParse(adjusterIdClaim.Value, out int adjusterId))
            {
                return Unauthorized("User identity not found.");
            }

            _logger.LogInformation("Sending WhatsApp message to {To} for ClaimId {ClaimId}", request.To, request.ClaimId);

            var result = await _communicationService.SendCommunicationAsync(request, adjusterId);

            return Ok(new
            {
                CommunicationId = result.CommunicationId,
                Warning = result.WarningMessage,
                Status = "Message queued"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message");
            return StatusCode(500, "Internal server error while sending WhatsApp.");
        }
    }
}
