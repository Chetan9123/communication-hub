using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunicationHub.Domain.Entities;
using CommunicationHub.Infrastructure.Data;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CommunicationHub.Infrastructure.Services;

public class ImapListeningService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImapListeningService> _logger;

    public ImapListeningService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ImapListeningService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IMAP Listening Service is starting.");

        var host = _configuration["Imap:Host"];
        var portStr = _configuration["Imap:Port"];
        var useSslStr = _configuration["Imap:UseSsl"];
        var username = _configuration["Imap:Username"];
        var password = _configuration["Imap:Password"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || !int.TryParse(portStr, out int port))
        {
            _logger.LogWarning("IMAP configuration is missing or invalid. Background service will stop.");
            return;
        }

        bool useSsl = bool.TryParse(useSslStr, out bool s) && s;

        // Create a long-running polling loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var client = new ImapClient();
                await client.ConnectAsync(host, port, useSsl, stoppingToken);
                await client.AuthenticateAsync(username, password, stoppingToken);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite, stoppingToken);

                // Fetch unread messages with Smart Filtering (Primary: Headers, Secondary: Subject)
                var query = SearchQuery.NotSeen.And(
                    SearchQuery.HeaderContains("In-Reply-To", "@commhub.local")
                    .Or(SearchQuery.HeaderContains("References", "@commhub.local"))
                    .Or(SearchQuery.SubjectContains("Claim #"))
                );

                var uids = await inbox.SearchAsync(query, stoppingToken);

                if (uids.Any())
                {
                    _logger.LogInformation("Found {Count} unread email(s) passing primary filter.", uids.Count);

                    var summaries = await inbox.FetchAsync(uids, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId | MessageSummaryItems.References, stoppingToken);

                    // Client-Side Smart Filter Context
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<CommunicationHubDbContext>();

                    foreach (var summary in summaries)
                    {
                        var fromAddress = summary.Envelope.From.Mailboxes.FirstOrDefault()?.Address?.ToLower();
                        if (fromAddress == null)
                        {
                            await inbox.AddFlagsAsync(summary.UniqueId, MessageFlags.Seen, true, stoppingToken);
                            continue;
                        }

                        InvolvedParty? targetParty = null;
                        int? resolvedClaimId = null;
                        int? resolvedPartyId = null;

                        // 1. Check headers: If In-Reply-To exists -> Get ClaimId/PartyId directly
                        string headerMatch = summary.Envelope.InReplyTo ?? string.Empty;
                        if (!string.IsNullOrEmpty(headerMatch) && headerMatch.Contains("@commhub.local"))
                        {
                            var parts = headerMatch.Trim('<', '>').Split('-');
                            if (parts.Length >= 2 && int.TryParse(parts[0], out int cid) && int.TryParse(parts[1], out int pid))
                            {
                                resolvedClaimId = cid;
                                resolvedPartyId = pid;
                            }
                        }

                        // 2. ELSE fallback: Match sender email with InvolvedParty
                        if (resolvedClaimId != null && resolvedPartyId != null)
                        {
                            targetParty = await context.InvolvedParties
                                .Include(p => p.Claim)
                                .ThenInclude(c => c!.ClaimAdjuster)
                                .FirstOrDefaultAsync(p => p.PartyId == resolvedPartyId && p.ClaimId == resolvedClaimId, stoppingToken);
                        }
                        else
                        {
                            targetParty = await context.InvolvedParties
                                .Include(p => p.Claim)
                                .ThenInclude(c => c!.ClaimAdjuster)
                                .OrderByDescending(p => p.PartyId)
                                .FirstOrDefaultAsync(p => p.Email != null && p.Email.ToLower() == fromAddress, stoppingToken);
                        }

                        // 3. Validate
                        bool isValid = false;
                        if (targetParty != null)
                        {
                            bool isSenderPartOfClaim = targetParty.ClaimId != null;
                            bool isSenderAssignedToAdjuster = targetParty.Claim?.ClaimAdjuster?.AdjusterId != null;

                            if (isSenderPartOfClaim && isSenderAssignedToAdjuster)
                            {
                                isValid = true;
                            }
                        }

                        // 4. IF valid -> Store communication
                        if (isValid)
                        {
                            var fullMessage = await inbox.GetMessageAsync(summary.UniqueId, stoppingToken);
                            await ProcessMessageAsync(fullMessage, targetParty!, context, stoppingToken);
                        }
                        // 5. ELSE -> Ignore email
                        else
                        {
                            _logger.LogInformation("Discarding email from {From}: Failed validation (Unassigned Claim or Unrecognized Sender).", fromAddress);
                        }

                        // Mark as read so we don't process it again
                        await inbox.AddFlagsAsync(summary.UniqueId, MessageFlags.Seen, true, stoppingToken);
                    }
                }

                await client.DisconnectAsync(true, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling IMAP server.");
            }

            // Waiting 30 seconds before polling again
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("IMAP Listening Service is stopping.");
    }

    private async Task ProcessMessageAsync(MimeMessage message, InvolvedParty party, CommunicationHubDbContext context, CancellationToken cancellationToken)
    {
        var subject = message.Subject;
        var textBody = message.TextBody;
        var htmlBody = message.HtmlBody;

        string senderEmail = party.Email?.ToLower() ?? "unknown";

        int claimId = party.ClaimId ?? 0;
        int partyId = party.PartyId;
        int adjusterId = party.Claim?.ClaimAdjuster?.AdjusterId ?? 0;

        _logger.LogInformation("IMAP: Processing strictly validated email from {SenderEmail} for ClaimId={ClaimId}, AdjusterId={AdjusterId}", senderEmail, claimId, adjusterId);

        var channel = await context.Channels
            .FirstOrDefaultAsync(c => c.Name == "Email", cancellationToken);

        string messageBodyContent = $"Subject: {subject}\n\n"
                           + (!string.IsNullOrWhiteSpace(textBody) ? textBody : htmlBody ?? string.Empty);

        var communication = new Communication
        {
            CommunicationId = Guid.NewGuid(),
            ClaimId = claimId == 0 ? null : claimId,
            PartyId = partyId,
            AdjusterId = adjusterId == 0 ? null : adjusterId,
            ChannelId = channel?.ChannelId,
            Direction = "Incoming",
            MessageBody = messageBodyContent,
            MessageType = "Email",
            Status = "Received",
            ReceivedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ReadAt = false,
            ReadAtDate = null
        };

        context.Communications.Add(communication);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("IMAP: Saved new communication from {SenderEmail} for ClaimId={ClaimId}", senderEmail, claimId);
    }

    private int? ExtractClaimIdFromSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return null;

        var prefix = "Claim #";
        int idx = subject.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var substring = subject.Substring(idx + prefix.Length);
            var numberSpan = substring.TakeWhile(char.IsDigit).ToArray();
            if (numberSpan.Length > 0 && int.TryParse(new string(numberSpan), out int claimId))
            {
                return claimId;
            }
        }
        return null;
    }
}
