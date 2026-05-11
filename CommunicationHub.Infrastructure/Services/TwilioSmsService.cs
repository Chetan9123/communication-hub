using System;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CommunicationHub.Infrastructure.Services;

/// <summary>
/// Twilio implementation of ISmsService.
/// Responsible for technical communication with the Twilio API.
/// Does not contain business logic or database persistence.
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        _logger = logger;
        
        _accountSid = configuration["Twilio:AccountSid"] ?? throw new ArgumentNullException("Twilio:AccountSid");
        _authToken = configuration["Twilio:AuthToken"] ?? throw new ArgumentNullException("Twilio:AuthToken");
        _fromNumber = configuration["Twilio:FromNumber"] ?? throw new ArgumentNullException("Twilio:FromNumber");

        // Initialize Twilio client
        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<bool> SendSmsAsync(string to, string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogError("Cannot send SMS: recipient phone number is null or empty.");
                return false;
            }

            _logger.LogInformation("Attempting to send SMS to {To}", to);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(to)
            );

            if (messageResource.ErrorCode != null)
            {
                _logger.LogError("Twilio error sending SMS: {ErrorCode} - {ErrorMessage}", 
                    messageResource.ErrorCode, messageResource.ErrorMessage);
                return false;
            }

            _logger.LogInformation("SMS sent successfully via Twilio. SID: {Sid}", messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending SMS via Twilio to {To}", to);
            return false;
        }
    }
}
