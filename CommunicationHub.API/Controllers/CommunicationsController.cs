using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.API.Security;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CommunicationsController : ControllerBase
{
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<CommunicationsController> _logger;

    public CommunicationsController(ICommunicationService communicationService, ILogger<CommunicationsController> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/communications/unread
    /// Populates the "To-Do" grid for the logged-in adjuster
    /// </summary>
    [HttpGet("unread")]
    public async Task<ActionResult<List<UnreadCommunicationDto>>> GetUnreadCommunications()
    {
        try
        {
            if (!User.TryGetAdjusterId(out var adjusterId))
                return Unauthorized(new { message = "Invalid token" });

            var unreadComms = await _communicationService.GetUnreadCommunicationsAsync(adjusterId);
            return Ok(unreadComms);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/communications/{commId}/read-status
    /// Marks a message as read/unread from the To-Do preview modal
    /// </summary>
    [HttpPut("{commId}/read-status")]
    public async Task<ActionResult<bool>> UpdateReadStatus(Guid commId, [FromBody] UpdateReadStatusRequest request)
    {
        try
        {
            var success = await _communicationService.UpdateReadStatusAsync(commId, request.IsRead);
            if (!success)
                return NotFound(new { message = "Communication not found" });

            return Ok(success);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/communications/claim/{claimId}/party/{partyId}
    /// Fetches the chronological timeline for the specific party view
    /// </summary>
    [HttpGet("claim/{claimId}/party/{partyId}")]
    public async Task<ActionResult<CommunicationThreadDto>> GetCommunicationThread(int claimId, int partyId)
    {
        try
        {
            var thread = await _communicationService.GetCommunicationThreadAsync(claimId, partyId);
            return Ok(thread);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/communications/claim/{claimId}/all
    /// Fetches the chronological timeline for the entire claim across all parties
    /// </summary>
    [HttpGet("claim/{claimId}/all")]
    public async Task<ActionResult<CommunicationThreadDto>> GetClaimCommunicationThread(int claimId)
    {
        try
        {
            var thread = await _communicationService.GetClaimCommunicationThreadAsync(claimId);
            return Ok(thread);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/communications/{commId}/notes
    /// Updates the editable summary notes for a specific message
    /// </summary>
    [HttpPut("{commId}/notes")]
    public async Task<ActionResult<bool>> UpdateNotes(Guid commId, [FromBody] UpdateNotesRequest request)
    {
        try
        {
            var success = await _communicationService.UpdateNotesAsync(commId, request.Notes ?? string.Empty);
            if (!success)
                return NotFound(new { message = "Communication not found" });

            return Ok(success);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/communications/send
    /// Dispatches the new message. Backend validates if the adjuster is assigned 
    /// and if the communication channel is enabled in the DB config.
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendCommunication([FromBody] SendCommunicationRequest request)
    {
        try
        {
            _logger.LogInformation("[CommunicationsController] Received send request for mode: {Mode}, ClaimId: {ClaimId}", request.Mode, request.ClaimId);

            if (!User.TryGetAdjusterId(out var adjusterId))
            {
                _logger.LogWarning("[CommunicationsController] Unauthorized send attempt.");
                return Unauthorized(new { message = "Invalid token" });
            }

            if (string.IsNullOrEmpty(request.Mode))
                return BadRequest(new { message = "Communication mode is required" });

            if (string.IsNullOrEmpty(request.Body))
                return BadRequest(new { message = "Message body is required" });

            var result = await _communicationService.SendCommunicationAsync(request, adjusterId);
            
            _logger.LogInformation("[CommunicationsController] Successfully processed {Mode} for ClaimId {ClaimId}. CommId: {CommId}", 
                request.Mode, request.ClaimId, result.CommunicationId);

            return Ok(new { communicationId = result.CommunicationId, warningMessage = result.WarningMessage });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "[CommunicationsController] Unauthorized access error.");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[CommunicationsController] Invalid operation error.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CommunicationsController] General error sending communication.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/communications/sync-twilio
    /// Manually triggers a synchronization of missing Twilio WhatsApp messages from the past 24 hours.
    /// Useful for recovering from server downtime or misconfigurations.
    /// </summary>
    [HttpPost("sync-twilio")]
    [AllowAnonymous]
    public async Task<ActionResult<int>> SyncTwilioMessages()
    {
        try
        {
            _logger.LogInformation("[CommunicationsController] Received manual request to sync missed Twilio messages.");
            
            // Only adjusters/admins should trigger this, no special parameters needed.
            var count = await _communicationService.SyncMissedTwilioMessagesAsync();
            
            return Ok(new { syncedCount = count, message = $"Successfully synchronized {count} missed messages." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CommunicationsController] Failed to sync missed Twilio messages.");
            return StatusCode(500, new { message = "An error occurred during synchronization." });
        }
    }

    /// <summary>
    /// GET /api/communications/channels
    /// Gets all enabled communication channels
    /// </summary>
    [HttpGet("channels")]
    public async Task<ActionResult<Dictionary<string, bool>>> GetEnabledChannels()
    {
        try
        {
            var channels = await _communicationService.GetEnabledChannelsAsync();
            return Ok(channels);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
