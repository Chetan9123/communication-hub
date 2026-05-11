using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationHub.API.Controllers;

[Route("api/webhooks/sms/receive")]
[ApiController]
[AllowAnonymous] // Twilio webhooks are public
public class SmsWebhookController : ControllerBase
{
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<SmsWebhookController> _logger;

    public SmsWebhookController(ICommunicationService communicationService, ILogger<SmsWebhookController> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/webhooks/sms/receive
    /// Entry point for Twilio SMS Webhooks via ngrok.
    /// Twilio sends data as application/x-www-form-urlencoded.
    /// </summary>
    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> ReceiveSms()
    {
        try
        {
            if (!Request.HasFormContentType)
            {
                _logger.LogWarning("[SmsWebhook] Received request without form content type.");
                return Content("<Response></Response>", "text/xml");
            }

            var form = await Request.ReadFormAsync();
            string from = form["From"].ToString() ?? string.Empty;
            string body = form["Body"].ToString() ?? string.Empty;
            string messageSid = form["MessageSid"].ToString() ?? form["SmsSid"].ToString() ?? "NO_SID";
            string numMediaStr = form["NumMedia"].ToString() ?? "0";
            
            _logger.LogInformation("[SmsWebhook] RECEIVED: From={From}, Body={Body}, SID={Sid}, NumMedia={NumMedia}", from, body, messageSid, numMediaStr);

            // Only reject if 'From' is missing. Allow empty body — Twilio sends Body=""
            // for image-only MMS messages (media with no text).
            if (string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("[SmsWebhook] Invalid request: 'From' is missing.");
                return Content("<Response></Response>", "text/xml");
            }

            // Extract media URLs safely
            var mediaUrls = new List<string>();
            if (int.TryParse(form["NumMedia"], out int numMedia))
            {
                for (int i = 0; i < numMedia; i++)
                {
                    string? mediaUrl = form[$"MediaUrl{i}"];
                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        mediaUrls.Add(mediaUrl);
                    }
                }
            }

            // Process the incoming message via CommunicationService
            var result = await _communicationService.ProcessIncomingSmsAsync(
                from, 
                body, 
                messageSid,
                mediaUrls);

            if (result)
            {
                _logger.LogInformation("[SmsWebhook] Successfully processed message from {From}", from);
            }
            else
            {
                _logger.LogError("[SmsWebhook] Failed to process message from {From}", from);
            }

            // Return empty TwiML to acknowledge receipt
            return Content("<Response></Response>", "text/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SmsWebhook] Exception occurred while receiving SMS");
            
            // Even on error, we return 200 OK with empty TwiML to prevent Twilio from 
            // retrying the same failing request repeatedly.
            return Content("<Response></Response>", "text/xml");
        }
    }
}