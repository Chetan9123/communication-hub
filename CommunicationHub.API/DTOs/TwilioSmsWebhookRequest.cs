namespace CommunicationHub.API.DTOs;

/// <summary>
/// DTO for incoming Twilio SMS webhook requests.
/// Parameters are sent as application/x-www-form-urlencoded.
/// </summary>
public class TwilioSmsWebhookRequest
{
    public string? MessageSid { get; set; }
    public string? SmsSid { get; set; }
    public string? AccountSid { get; set; }
    public string? MessagingServiceSid { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Body { get; set; }
    public int NumMedia { get; set; }
}
