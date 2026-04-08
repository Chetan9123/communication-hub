using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.API.Security;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    /// <summary>
    /// GET /api/claims/{claimId}
    /// Gets claim details with involved parties
    /// </summary>
    [HttpGet("{claimId}")]
    public async Task<ActionResult<ClaimDetailsDto>> GetClaimDetails(int claimId)
    {
        try
        {
            var claimDetails = await _claimService.GetClaimDetailsAsync(claimId);
            if (claimDetails.ClaimId == 0)
                return NotFound(new { message = "Claim not found" });

            return Ok(claimDetails);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/claims/{claimId}/parties
    /// Fetches involved parties to display the hub icons
    /// </summary>
    [HttpGet("{claimId}/parties")]
    public async Task<ActionResult<List<InvolvedPartyDto>>> GetInvolvedParties(int claimId)
    {
        try
        {
            var parties = await _claimService.GetInvolvedPartiesAsync(claimId);
            return Ok(parties);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/claims/assigned-to-adjuster
    /// Gets all claims assigned to an adjuster
    /// </summary>
    [HttpGet("assigned-to-adjuster")]
    public async Task<ActionResult<List<AssignedClaimDto>>> GetAssignedClaims()
    {
        try
        {
            if (!User.TryGetAdjusterId(out var adjusterId))
                return Unauthorized(new { message = "Invalid token" });

            var assignedClaims = await _claimService.GetAssignedClaimsAsync(adjusterId);
            return Ok(assignedClaims);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
