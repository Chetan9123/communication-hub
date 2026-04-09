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
    public async Task<IActionResult> ReceiveSms([FromForm] TwilioSmsWebhookRequest request)
    {
        _logger.LogInformation("[SmsWebhook] RECEIVED: From={From}, Body={Body}", request.From, request.Body);

        try
        {
            if (string.IsNullOrEmpty(request.From) || string.IsNullOrEmpty(request.Body))
            {
                _logger.LogWarning("[SmsWebhook] Invalid request received. From or Body is missing.");
                // Still return 200 OK with empty TwiML to avoid Twilio retries for bad data
                return Content("<Response></Response>", "text/xml");
            }

            // Process the incoming message via CommunicationService
            // We pass the SmsSid or MessageSid as the unique provider reference
            var result = await _communicationService.ProcessIncomingSmsAsync(
                request.From, 
                request.Body, 
                request.MessageSid ?? request.SmsSid ?? string.Empty);

            if (result)
            {
                _logger.LogInformation("[SmsWebhook] Successfully processed message from {From}", request.From);
            }
            else
            {
                _logger.LogError("[SmsWebhook] Failed to process message from {From}", request.From);
            }

            // Return empty TwiML to acknowledge receipt
            return Content("<Response></Response>", "text/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SmsWebhook] Exception occurred while receiving SMS from {From}", request.From);
            
            // Even on error, we return 200 OK with empty TwiML to prevent Twilio from 
            // retrying the same failing request repeatedly.
            return Content("<Response></Response>", "text/xml");
        }
    }
}