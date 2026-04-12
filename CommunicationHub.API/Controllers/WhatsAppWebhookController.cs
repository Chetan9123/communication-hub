using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace CommunicationHub.API.Controllers;

[ApiController]
[Route("api/webhooks/whatsapp")]
public class WhatsAppWebhookController : TwilioController
{
    private readonly ICommunicationService _communicationService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(ICommunicationService communicationService, ILogger<WhatsAppWebhookController> logger)
    {
        _communicationService = communicationService;
        _logger = logger;
    }

    /// <summary>
    /// GET test endpoint to verify the webhook is reachable.
    /// URL: /api/webhooks/whatsapp/receive
    /// </summary>
    [HttpGet("receive")]
    public IActionResult TestReceive()
    {
        return Ok("WhatsApp Webhook is active and listening. Use POST for Twilio requests.");
    }

    /// <summary>
    /// Receives incoming WhatsApp messages from Twilio.
    /// URL: /api/webhooks/whatsapp/receive
    /// </summary>
    [HttpPost("receive")]
    [ValidateRequest]
    public async Task<IActionResult> Receive()
    {
        var twiml = new MessagingResponse();

        try
        {
            if (!Request.HasFormContentType)
            {
                _logger.LogWarning("Webhook: Received request without form content type.");
                return TwiML(twiml);
            }

            var form = await Request.ReadFormAsync();
            
            // Extract core fields with safe null handling
            string from = form["From"].ToString() ?? string.Empty;
            string body = form["Body"].ToString() ?? string.Empty;
            string messageSid = form["MessageSid"].ToString() ?? "NO_SID";
            
            _logger.LogInformation("Webhook: Processing WhatsApp. From: {From}, SID: {Sid}", from, messageSid);

            if (string.IsNullOrEmpty(from))
            {
                _logger.LogWarning("Webhook: Missing 'From' number. Acknowledging anyway to stop Twilio retries.");
                return TwiML(twiml);
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

            // Process the message via CommunicationService
            try 
            {
                await _communicationService.ProcessIncomingWhatsAppAsync(from, body, messageSid, mediaUrls);
            }
            catch (Exception serviceEx)
            {
                _logger.LogError(serviceEx, "Webhook: Internal service error while processing message {Sid}", messageSid);
            }

            return TwiML(twiml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook: Critical failure in WhatsApp receive controller.");
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Response></Response>", "text/xml");
        }
    }

    /// <summary>
    /// GET test endpoint to verify the status webhook is reachable.
    /// URL: /api/webhooks/whatsapp/status
    /// </summary>
    [HttpGet("status")]
    public IActionResult TestStatus()
    {
        return Ok("WhatsApp Status Webhook is active and listening. Use POST for Twilio status callbacks.");
    }

    /// <summary>
    /// Receives status updates (Sent, Delivered, Failed) from Twilio.
    /// URL: /api/webhooks/whatsapp/status
    /// </summary>
    [HttpPost("status")]
    // [ValidateRequest] // Optional: Twilio status callbacks are also signed
    public async Task<IActionResult> Status([FromForm] string MessageSid, [FromForm] string MessageStatus)
    {
        try
        {
            _logger.LogInformation("Webhook: WhatsApp Status Change. SID: {Sid}, Status: {Status}", MessageSid, MessageStatus);

            await _communicationService.UpdateCommunicationStatusBySidAsync(MessageSid, MessageStatus);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WhatsApp status webhook");
            return StatusCode(500);
        }
    }
}
