using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunicationHub.API.Security;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Sends an outbound email via SMTP (MailKit) and records it as a Communication.
    /// POST /api/email/send
    /// </summary>
    [HttpPost("send")]
    [Authorize]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        if (!User.TryGetAdjusterId(out int adjusterId))
            return Unauthorized(new { message = "Valid Adjuster ID could not be resolved from token." });

        _logger.LogInformation(
            "AdjusterId={AdjusterId} sending email to {To} for ClaimId={ClaimId}",
            adjusterId, request.To, request.ClaimId);

        var result = await _emailService.SendEmailAsync(
            request.ClaimId,
            request.PartyId,
            request.To,
            request.Subject,
            request.Body,
            adjusterId,
            HttpContext.RequestAborted);

        if (result.Sent)
            return Ok(new { message = "Email sent and recorded successfully.", communicationId = result.CommunicationId });

        // Still a 200 — the record is saved; only the actual send may have failed
        return Ok(new
        {
            message = "Email saved to database but could not be transmitted via SMTP. Check server logs for details."
        });
    }

    /// <summary>
    /// SendGrid Inbound Parse webhook endpoint.
    /// Receives multipart/form-data and saves the inbound email as a Communication record.
    /// POST /api/email/inbound
    /// </summary>
    [HttpPost("inbound")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveInboundEmail(
        [FromForm] string from,
        [FromForm] string subject,
        [FromForm] string? text,
        [FromForm] string? html)
    {
        _logger.LogInformation("Inbound email webhook received. Subject: {Subject}", subject);

        var success = await _emailService.ProcessInboundEmailAsync(
            from    ?? string.Empty,
            subject ?? string.Empty,
            text    ?? string.Empty,
            html,
            HttpContext.RequestAborted);

        if (success)
            return Ok(new { message = "Inbound email processed and saved." });

        return StatusCode(500, new { message = "Failed to process inbound email. Check server logs." });
    }
}

/// <summary>Request body for POST /api/email/send</summary>
public class SendEmailRequest
{
    [Required] public int ClaimId { get; set; }
    [Required] public int PartyId { get; set; }
    [Required, EmailAddress] public string To { get; set; } = string.Empty;
    [Required] public string Subject { get; set; } = string.Empty;
    [Required] public string Body { get; set; } = string.Empty;
}
