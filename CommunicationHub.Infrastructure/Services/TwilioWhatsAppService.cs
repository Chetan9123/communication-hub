using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CommunicationHub.Infrastructure.Services;

public class TwilioWhatsAppService : IWhatsAppService
{
    private readonly ILogger<TwilioWhatsAppService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromWhatsAppNumber;

    public TwilioWhatsAppService(IConfiguration configuration, ILogger<TwilioWhatsAppService> logger)
    {
        _logger = logger;
        _accountSid = configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
        _authToken = configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
        
        // WhatsApp number should be in format "whatsapp:+14155238886"
        var fromNumber = configuration["Twilio:WhatsAppNumber"] ?? throw new ArgumentNullException("Twilio:WhatsAppNumber");
        _fromWhatsAppNumber = fromNumber.StartsWith("whatsapp:") ? fromNumber : $"whatsapp:{fromNumber}";

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<WhatsAppSendResult> SendWhatsAppAsync(string to, string message, IEnumerable<string>? mediaUrls = null, string? statusCallback = null)
    {
        try
        {
            // Normalize recipient number to include whatsapp: prefix
            var toWhatsApp = to.StartsWith("whatsapp:") ? to : $"whatsapp:{to}";
            
            _logger.LogInformation("Attempting to send WhatsApp message to {To}", toWhatsApp);

            var media = mediaUrls?.Select(url => new Uri(url)).ToList();

            var messageOptions = new CreateMessageOptions(new PhoneNumber(toWhatsApp))
            {
                From = new PhoneNumber(_fromWhatsAppNumber),
                Body = message,
                MediaUrl = media,
                StatusCallback = !string.IsNullOrEmpty(statusCallback) ? new Uri(statusCallback) : null
            };

            var messageResource = await MessageResource.CreateAsync(messageOptions);

            if (messageResource.ErrorCode != null)
            {
                _logger.LogError("Twilio error sending WhatsApp: {ErrorCode} - {ErrorMessage}", 
                    messageResource.ErrorCode, messageResource.ErrorMessage);
                
                return new WhatsAppSendResult 
                { 
                    Success = false, 
                    ErrorCode = messageResource.ErrorCode, 
                    ErrorMessage = messageResource.ErrorMessage 
                };
            }

            _logger.LogInformation("WhatsApp message sent successfully via Twilio. SID: {Sid}", messageResource.Sid);
            
            return new WhatsAppSendResult 
            { 
                Success = true, 
                Sid = messageResource.Sid 
            };
        }
        catch (Twilio.Exceptions.ApiException apiEx)
        {
            _logger.LogError(apiEx, "Twilio API Exception while sending WhatsApp to {To}. Status: {Status}, Code: {Code}, Details: {Msg}", 
                to, apiEx.Status, apiEx.Code, apiEx.Message);
            
            return new WhatsAppSendResult 
            { 
                Success = false, 
                ErrorCode = apiEx.Code, 
                ErrorMessage = $"Twilio Error {apiEx.Code}: {apiEx.Message}" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "General Exception occurred while sending WhatsApp via Twilio to {To}", to);
            
            return new WhatsAppSendResult 
            { 
                Success = false, 
                ErrorMessage = $"Critical Error: {ex.Message}" 
            };
        }
    }
}
