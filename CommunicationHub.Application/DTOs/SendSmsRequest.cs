using System.ComponentModel.DataAnnotations;

namespace CommunicationHub.Application.DTOs;

/// <summary>
/// DTO for SMS sending requests from the API.
/// Incorporates business data (ClaimId, PartyId) for logging purposes.
/// </summary>
public class SendSmsRequest
{
    /// <summary>
    /// ID of the claim associated with this communication.
    /// </summary>
    [Required]
    public int ClaimId { get; set; }

    /// <summary>
    /// ID of the involved party associated with this communication.
    /// </summary>
    [Required]
    public int PartyId { get; set; }

    /// <summary>
    /// Recipient phone number in international format (e.g. +91XXXXXXXXXX).
    /// </summary>
    [Required]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "Phone number must be in international format (e.g. +91XXXXXXXXXX)")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// The content of the SMS message.
    /// </summary>
    [Required]
    [StringLength(1600, ErrorMessage = "SMS content cannot exceed 1600 characters.")]
    public string Message { get; set; } = string.Empty;
}
