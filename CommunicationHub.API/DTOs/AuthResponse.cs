namespace CommunicationHub.API.DTOs;

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public AdjusterDto? User { get; set; }
}
