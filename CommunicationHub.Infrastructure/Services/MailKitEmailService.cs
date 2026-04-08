using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Domain.Entities;
using CommunicationHub.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CommunicationHub.Infrastructure.Services;

/// <summary>
/// Sends outbound emails via SMTP (MailKit) and processes inbound email records.
/// Supports HTML bodies, multiple recipients, and optional attachments.
/// Includes exponential-backoff retry (3 attempts) and comprehensive logging.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly CommunicationHubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailService> _logger;

    private const int MaxRetries   = 3;
    private const int BaseDelayMs  = 500; // doubles each attempt

    public MailKitEmailService(
        CommunicationHubDbContext context,
        IConfiguration configuration,
        ILogger<MailKitEmailService> logger)
    {
        _context       = context;
        _configuration = configuration;
        _logger        = logger;
    }

    // ── PUBLIC: Send outbound email ────────────────────────────────────────────

    public async Task<(bool Sent, Guid CommunicationId)> SendEmailAsync(
        int claimId,
        int partyId,
        string to,
        string subject,
        string body,
        int adjusterId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Read & validate SMTP configuration ──────────────────────────────
        var smtpConfig = ReadSmtpConfig();

        // Config is valid when host, email and password are set and not placeholder values
        bool configOk = smtpConfig != null
                        && !string.IsNullOrWhiteSpace(smtpConfig.Host)
                        && !string.IsNullOrWhiteSpace(smtpConfig.FromEmail)
                        && !string.IsNullOrWhiteSpace(smtpConfig.Password)
                        && smtpConfig.Password != "your-gmail-app-password"
                        && smtpConfig.Password != "your-app-password";

        if (!configOk)
        {
            _logger.LogWarning(
                "SMTP is not fully configured. Email will be stored as 'Stored' but NOT transmitted.");
        }

        // ── 2. Attempt SMTP send (with retry) ──────────────────────────────────
        bool   sent        = false;
        string sendStatus  = "Stored";
        string? errorDetail = null;

        if (configOk && smtpConfig != null)
        {
            (sent, sendStatus, errorDetail) = await SendWithRetryAsync(
                claimId, partyId, smtpConfig, to, subject, body, cancellationToken);
        }

        // ── 3. Persist communication record regardless of send outcome ─────────
        return await PersistCommunicationAsync(
            claimId, partyId, adjusterId, subject, body,
            sent, sendStatus, errorDetail, cancellationToken);
    }

    // ── PUBLIC: Process inbound email (webhook) ────────────────────────────────

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
            _logger.LogInformation(
                "Extracted ClaimId={ClaimId} from subject", claimId?.ToString() ?? "none");

            string senderEmail = ExtractEmailAddress(from);
            
            // Link Claim and ClaimAdjuster to fetch the assigned AdjusterId
            var party = await _context.InvolvedParties
                .Include(p => p.Claim)
                .ThenInclude(c => c!.ClaimAdjuster)
                .FirstOrDefaultAsync(
                    p => p.Email != null && p.Email.ToLower() == senderEmail.ToLower(),
                    cancellationToken);

            int? finalClaimId = claimId ?? party?.ClaimId;
            int? adjusterId = null;

            if (party != null)
            {
                _logger.LogInformation("Matched sender to PartyId={PartyId}", party.PartyId);
                var assignment = party.Claim?.ClaimAdjuster;
                adjusterId = assignment?.AdjusterId;
            }
            else
            {
                _logger.LogWarning("No InvolvedParty matched sender email '{Sender}'", senderEmail);
            }

            var channel = await _context.Channels
                .FirstOrDefaultAsync(c => c.Name == "Email", cancellationToken);

            string messageBody = $"Subject: {subject}\n\n"
                               + (!string.IsNullOrWhiteSpace(text) ? text : html ?? string.Empty);

            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId         = finalClaimId,
                PartyId         = party?.PartyId,
                AdjusterId      = adjusterId,
                ChannelId       = channel?.ChannelId,
                Direction       = "Incoming",
                MessageBody     = messageBody,
                MessageType     = "Email",
                Status          = "Received",
                ReceivedAt      = DateTime.UtcNow,
                IsActive        = true,
                CreatedAt       = DateTime.UtcNow,
                ReadAt          = false,
                ReadAtDate      = null
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

    // ── PRIVATE: Core SMTP logic with retry ────────────────────────────────────

    private async Task<(bool Sent, string Status, string? ErrorDetail)> SendWithRetryAsync(
        int claimId,
        int partyId,
        SmtpConfig config,
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                return (false, "Failed", "Request was cancelled.");

            _logger.LogInformation(
                "SMTP send attempt {Attempt}/{Max}: To={To} Subject={Subject}",
                attempt, MaxRetries, to, subject);

            try
            {
                var message = BuildMimeMessage(claimId, partyId, config, to, subject, body);

                using var smtp = new SmtpClient();

                // Choose SSL/TLS mode
                var secureOption = config.UseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                await smtp.ConnectAsync(config.Host, config.Port, secureOption, cancellationToken);
                _logger.LogDebug("Connected to SMTP host {Host}:{Port}", config.Host, config.Port);

                await smtp.AuthenticateAsync(config.Username, config.Password, cancellationToken);
                _logger.LogDebug("Authenticated as {Username}", config.Username);

                await smtp.SendAsync(message, cancellationToken);
                await smtp.DisconnectAsync(quit: true, cancellationToken);

                _logger.LogInformation("Email sent successfully to {To} on attempt {Attempt}", to, attempt);
                return (true, "Sent", null);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("SMTP send cancelled on attempt {Attempt}", attempt);
                return (false, "Failed", "Request was cancelled.");
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                int delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1); // 500ms, 1000ms, ...
                _logger.LogWarning(
                    ex,
                    "SMTP send failed on attempt {Attempt}/{Max}. Retrying in {Delay}ms. Error: {Message}",
                    attempt, MaxRetries, delay, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP send failed permanently after {Max} attempts. Error: {Message}",
                    MaxRetries, ex.Message);
                return (false, "Failed", ex.Message);
            }
        }

        return (false, "Failed", "All retry attempts exhausted.");
    }

    // ── PRIVATE: Build MimeMessage (HTML + optional attachments) ──────────────

    private static MimeMessage BuildMimeMessage(
        int claimId,
        int partyId,
        SmtpConfig config,
        string to,
        string subject,
        string htmlBody,
        IEnumerable<(string FileName, byte[] Data, string ContentType)>? attachments = null)
    {
        var message = new MimeMessage();
        
        // Define standard mapping headers
        message.MessageId = $"{claimId}-{partyId}-{Guid.NewGuid():N}@commhub.local";
        message.Headers.Add("X-Conversation-Id", $"{claimId}-{partyId}");

        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));

        // Support comma-separated recipients
        foreach (var address in to.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            message.To.Add(MailboxAddress.Parse(address));

        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody  = htmlBody,
            TextBody  = StripHtml(htmlBody) // plain-text fallback
        };

        if (attachments != null)
        {
            foreach (var (fileName, data, contentType) in attachments)
                builder.Attachments.Add(fileName, data, ContentType.Parse(contentType));
        }

        message.Body = builder.ToMessageBody();
        return message;
    }

    // ── PRIVATE: Persist communication record ─────────────────────────────────

    private async Task<(bool Sent, Guid CommunicationId)> PersistCommunicationAsync(
        int claimId,
        int partyId,
        int adjusterId,
        string subject,
        string body,
        bool sent,
        string sendStatus,
        string? errorDetail,
        CancellationToken cancellationToken)
    {
        try
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(c => c.Name == "Email", cancellationToken);

            if (channel == null)
                _logger.LogWarning("'Email' channel not found in Channel table. Saving without ChannelId.");

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

            return (sent, communication.CommunicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Communication record for ClaimId={ClaimId}", claimId);
            return (false, Guid.Empty);
        }
    }

    // ── PRIVATE: Configuration helper ─────────────────────────────────────────

    private SmtpConfig? ReadSmtpConfig()
    {
        var section = _configuration.GetSection("Smtp");
        if (!section.Exists()) return null;

        return new SmtpConfig
        {
            Host      = section["Host"]      ?? string.Empty,
            Port      = int.TryParse(section["Port"], out var port) ? port : 587,
            Username  = section["Username"]  ?? string.Empty,
            Password  = section["Password"]  ?? string.Empty,
            FromEmail = section["FromEmail"] ?? string.Empty,
            FromName  = section["FromName"]  ?? "Communication Hub",
            UseSsl    = bool.TryParse(section["UseSsl"], out var ssl) && ssl
        };
    }

    // ── PRIVATE: Helpers ───────────────────────────────────────────────────────

    private static string ExtractEmailAddress(string from)
    {
        if (string.IsNullOrWhiteSpace(from)) return string.Empty;
        var match = Regex.Match(from, @"<([^>]+)>");
        return match.Success ? match.Groups[1].Value.Trim() : from.Trim();
    }

    private static int? ExtractClaimIdFromSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return null;
        var match = Regex.Match(subject, @"Claim\s*#?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            return id;
        return null;
    }

    /// <summary>Strips HTML tags to produce a plain-text fallback.</summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return Regex.Replace(html, "<[^>]+>", string.Empty).Trim();
    }

    // ── Inner record: SMTP settings ───────────────────────────────────────────

    private sealed record SmtpConfig
    {
        public string Host      { get; init; } = string.Empty;
        public int    Port      { get; init; } = 587;
        public string Username  { get; init; } = string.Empty;
        public string Password  { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;
        public string FromName  { get; init; } = "Communication Hub";
        public bool   UseSsl    { get; init; }
    }
}
