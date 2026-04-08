using System;
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
public class UsersController : ControllerBase
{
    private readonly IAdjusterService _adjusterService;

    public UsersController(IAdjusterService adjusterService)
    {
        _adjusterService = adjusterService;
    }

    /// <summary>
    /// GET /api/users/dashboard
    /// Gets the adjuster dashboard information
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdjusterDashboardDto>> GetDashboard()
    {
        try
        {
            if (!User.TryGetAdjusterId(out var adjusterId))
                return Unauthorized(new { message = "Invalid token" });

            var dashboard = await _adjusterService.GetDashboardAsync(adjusterId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
