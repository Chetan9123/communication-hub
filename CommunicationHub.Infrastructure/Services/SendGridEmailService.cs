using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using CommunicationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CommunicationHub.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly CommunicationHubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        CommunicationHubDbContext context,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Sent, Guid CommunicationId)> SendEmailAsync(
        int claimId,
        int partyId,
        string to,
        string subject,
        string body,
        int adjusterId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Validate configuration ──────────────────────────────────────────
        var apiKey    = _configuration["SendGrid:ApiKey"];
        var fromEmail = _configuration["SendGrid:FromEmail"];
        var fromName  = _configuration["SendGrid:FromName"] ?? "Communication Hub";

        bool configOk = !string.IsNullOrWhiteSpace(apiKey)
                        && apiKey != "YOUR_SENDGRID_API_KEY"
                        && !string.IsNullOrWhiteSpace(fromEmail);

        if (!configOk)
            _logger.LogWarning("SendGrid is not fully configured. Email will be stored but NOT transmitted.");

        // ── 2. Call SendGrid ───────────────────────────────────────────────────
        bool sent        = false;
        string sendStatus = "Stored"; // default when not configured or send fails
        string? errorDetail = null;

        if (configOk)
        {
            try
            {
                var client      = new SendGridClient(apiKey!);
                var fromAddress = new EmailAddress(fromEmail, fromName);
                var toAddress   = new EmailAddress(to);

                // Plain-text and HTML body kept separate
                var msg = MailHelper.CreateSingleEmail(
                    from:        fromAddress,
                    to:          toAddress,
                    subject:     subject,
                    plainTextContent: body,
                    htmlContent: $"<p>{WebUtility.HtmlEncode(body).Replace("\n", "<br/>")}</p>");

                _logger.LogInformation(
                    "Calling SendGrid: From={From} To={To} Subject={Subject}",
                    fromEmail, to, subject);

                var response = await client.SendEmailAsync(msg, cancellationToken);

                // ── Read response body (crucial for diagnosing rejections) ──
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);

                _logger.LogInformation(
                    "SendGrid response: StatusCode={StatusCode} Body={Body}",
                    (int)response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    sent       = true;
                    sendStatus = "Sent";
                    _logger.LogInformation("Email sent successfully to {To}", to);
                }
                else
                {
                    sendStatus  = "Failed";
                    errorDetail = $"HTTP {(int)response.StatusCode}: {responseBody}";

                    _logger.LogError(
                        "SendGrid rejected the email. StatusCode={StatusCode} Details={Details}",
                        (int)response.StatusCode, responseBody);
                }
            }
            catch (OperationCanceledException)
            {
                sendStatus  = "Failed";
                errorDetail = "Request was cancelled.";
                _logger.LogWarning("SendGrid request was cancelled for ClaimId={ClaimId}", claimId);
            }
            catch (Exception ex)
            {
                sendStatus  = "Failed";
                errorDetail = ex.Message;
                _logger.LogError(ex, "Unexpected error calling SendGrid API for ClaimId={ClaimId}", claimId);
            }
        }

        // ── 3. Persist the communication record regardless of send outcome ────
        try
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(c => c.Name == "Email", cancellationToken);

            if (channel == null)
                _logger.LogWarning("'Email' channel not found in Channel table. Saving without ChannelId.");

            // Subject embedded in body because Communication table has no Subject column
            string combinedBody = $"Subject: {subject}\n\n{body}";

            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId         = claimId,
                PartyId         = partyId,
                ChannelId       = channel?.ChannelId,
                AdjusterId      = adjusterId,
                Direction       = "Outgoing",
                MessageBody     = combinedBody,
                MessageType     = "Email",
                Status          = sendStatus,
                ErrorMessage    = errorDetail,
                SentAt          = DateTime.UtcNow,
                IsActive        = true,
                CreatedAt       = DateTime.UtcNow,
                ReadAt          = true,
                ReadAtDate      = DateTime.UtcNow
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Communication record saved. CommunicationId={Id} Status={Status}",
                communication.CommunicationId, sendStatus);

            return (sent, communication.CommunicationId); // true only when SendGrid actually accepted the email
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Communication record for ClaimId={ClaimId}", claimId);
            return (false, Guid.Empty);
        }
    }

    public async Task<bool> ProcessInboundEmailAsync(
        string from,
        string subject,
        string text,
        string? html,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Inbound email received. From={From} Subject={Subject}", from, subject);

            int? claimId = ExtractClaimIdFromSubject(subject);
            _logger.LogInformation("Extracted ClaimId={ClaimId} from subject", claimId?.ToString() ?? "none");

            // Match sender to an InvolvedParty using exact email comparison, not substring
            string senderEmail = ExtractEmailAddress(from);
            var party = await _context.InvolvedParties
                .FirstOrDefaultAsync(
                    p => p.Email != null && p.Email.ToLower() == senderEmail.ToLower(),
                    cancellationToken);

            if (party != null)
                _logger.LogInformation("Matched sender to PartyId={PartyId}", party.PartyId);
            else
                _logger.LogWarning("No InvolvedParty matched sender email '{Sender}'", senderEmail);

            var channel = await _context.Channels
                .FirstOrDefaultAsync(c => c.Name == "Email", cancellationToken);

            string messageBody = $"Subject: {subject}\n\n"
                               + (!string.IsNullOrWhiteSpace(text) ? text : html ?? string.Empty);

            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId         = claimId ?? party?.ClaimId,
                PartyId         = party?.PartyId,
                ChannelId       = channel?.ChannelId,
                Direction       = "Incoming",
                MessageBody     = messageBody,
                MessageType     = "Email",
                Status          = "Received",
                ReceivedAt      = DateTime.UtcNow,
                IsActive        = true,
                CreatedAt       = DateTime.UtcNow
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Inbound email saved. CommunicationId={Id} ClaimId={ClaimId}",
                communication.CommunicationId, communication.ClaimId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process inbound email from '{From}'", from);
            return false;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the email address from a "Display Name &lt;email@domain.com&gt;" string.
    /// Falls back to returning the raw input.
    /// </summary>
    private static string ExtractEmailAddress(string from)
    {
        if (string.IsNullOrWhiteSpace(from)) return string.Empty;

        var match = Regex.Match(from, @"<([^>]+)>");
        return match.Success ? match.Groups[1].Value.Trim() : from.Trim();
    }

    /// <summary>
    /// Extracts ClaimId from subjects like "Claim #123 Update" or "Re: Claim 45".
    /// </summary>
    private static int? ExtractClaimIdFromSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return null;

        var match = Regex.Match(subject, @"Claim\s*#?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            return id;

        return null;
    }
}
