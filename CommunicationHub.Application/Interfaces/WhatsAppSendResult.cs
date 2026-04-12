namespace CommunicationHub.Application.Interfaces;

public class WhatsAppSendResult
{
    public bool Success { get; set; }
    public string? Sid { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ErrorCode { get; set; }
}
