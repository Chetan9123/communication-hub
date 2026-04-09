using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using CommunicationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunicationHub.Infrastructure.Services;

public class CommunicationService : ICommunicationService
{
    private readonly CommunicationHubDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<CommunicationService> _logger;

    public CommunicationService(
        CommunicationHubDbContext context, 
        IEmailService emailService, 
        ISmsService smsService,
        ILogger<CommunicationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<List<UnreadCommunicationDto>> GetUnreadCommunicationsAsync(int adjusterId)
    {
        var unreadMessages = await _context.Communications
            .Where(c => c.Adjuster!.AdjusterId == adjusterId && (c.ReadAt == null || c.ReadAt == false) && c.IsActive.HasValue && c.IsActive.Value)
            .Include(c => c.Claim)
            .Include(c => c.Party)
            .Include(c => c.Channel)
            .OrderByDescending(c => c.ReceivedAt)
            .ToListAsync();

        return unreadMessages.Select(m => new UnreadCommunicationDto
        {
            CommunicationId = m.CommunicationId,
            ClaimId = m.ClaimId.HasValue ? m.ClaimId.Value : 0,
            ClaimNumber = m.Claim?.ClaimNumber,
            PolicyNumber = m.Claim?.PolicyNumber,
            PartyId = m.PartyId.HasValue ? m.PartyId.Value : 0,
            SenderName = m.Party != null ? $"{m.Party.FirstName} {m.Party.LastName}".Trim() : string.Empty,
            CommunicationMode = m.Channel?.Name,
            MessagePreview = TruncateMessage(m.MessageBody, 100),
            ReceivedAt = m.ReceivedAt,
            IsRead = m.ReadAt.HasValue && m.ReadAt.Value,
            Status = m.Status
        }).ToList();
    }

    public async Task<bool> UpdateReadStatusAsync(Guid communicationId, bool isRead)
    {
        var communication = await _context.Communications.FindAsync(communicationId);
        if (communication == null)
            return false;

        communication.ReadAt = isRead;
        communication.ReadAtDate = isRead ? DateTime.UtcNow : null;

        // If newly read, update the Status string to reflect it
        if (isRead && (communication.Status == "Sent" || communication.Status == "Received" || string.IsNullOrEmpty(communication.Status)))
        {
            communication.Status = "Read";
        }

        _context.Communications.Update(communication);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CommunicationThreadDto> GetCommunicationThreadAsync(int claimId, int partyId)
    {
        var claim = await _context.Claims
            .Include(c => c.Communications)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return new CommunicationThreadDto();

        var party = await _context.InvolvedParties
            .FirstOrDefaultAsync(p => p.PartyId == partyId && p.ClaimId == claimId);

        if (party == null)
            return new CommunicationThreadDto();

        var messages = await _context.Communications
            .Where(c => c.ClaimId == claimId && c.PartyId == partyId && c.IsActive.HasValue && c.IsActive.Value)
            .Include(c => c.MessageAttachments)
            .Include(c => c.Channel)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return new CommunicationThreadDto
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            PolicyNumber = claim.PolicyNumber,
            PartyId = party.PartyId,
            PartyName = $"{party.FirstName} {party.LastName}".Trim(),
            Messages = messages.Select(m => new CommunicationMessageDto
            {
                CommunicationId = m.CommunicationId,
                Direction = m.Direction,
                Timestamp = m.SentAt ?? m.ReceivedAt ?? m.CreatedAt,
                Mode = m.Channel?.Name,
                MessageBody = m.MessageBody,
                Status = m.Status,
                IsRead = m.ReadAt.HasValue && m.ReadAt.Value,
                Notes = null,
                Attachments = m.MessageAttachments.Select(a => new AttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileUrl = a.FileUrl,
                    MimeType = a.MimeType,
                    FileSize = a.FileSize
                }).ToList()
            }).ToList()
        };
    }

    public async Task<bool> UpdateNotesAsync(Guid communicationId, string notes)
    {
        // The current database schema does not have a Notes column; gracefully return.
        return await Task.FromResult(false);
    }

    public async Task<(Guid CommunicationId, string? WarningMessage)> SendCommunicationAsync(SendCommunicationRequest request, int adjusterId)
    {
        // Validate adjuster access
        var hasAccess = await ValidateAdjusterAccessAsync(adjusterId, request.ClaimId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("Adjuster is not assigned to this claim.");

        // Validate channel is enabled
        var enabledChannels = await GetEnabledChannelsAsync();
        bool isChannelEnabled = enabledChannels.ContainsKey(request.Mode!) && enabledChannels[request.Mode!];

        var channel = await _context.Channels
            .FirstOrDefaultAsync(c => c.Name == request.Mode);

        if (channel == null)
            throw new InvalidOperationException($"Communication channel '{request.Mode}' not found.");

        var body = request.Body;
        if (!string.IsNullOrEmpty(request.Signature))
        {
            body = $"{body}\n\n---\n{request.Signature}";
        }

        Guid assignedCommunicationId;
        string? warningMsg = isChannelEnabled ? null : $"Warning: The '{request.Mode}' channel is currently disabled. The message was stored but not transmitted.";

        // If the mode is Email, delegate entirely to our specialized IEmailService
        // (It already handles the database logging)
        if (request.Mode == "Email")
        {
            var result = await _emailService.SendEmailAsync(
                request.ClaimId,
                request.PartyId,
                request.To,
                request.Subject ?? "Communication Update", // fallback subject
                body,
                adjusterId);

            assignedCommunicationId = result.CommunicationId;
            if (!result.Sent && isChannelEnabled)
                warningMsg = "Warning: The email was saved to the database but SendGrid failed to transmit it.";
        }
        else if (request.Mode == "SMS")
        {
            _logger.LogInformation("[CommunicationService] Attempting to send SMS to {To} for ClaimId {ClaimId}", request.To, request.ClaimId);
            
            bool isSent = false;
            if (isChannelEnabled)
            {
                isSent = await _smsService.SendSmsAsync(request.To, body);
                if (isSent)
                {
                    _logger.LogInformation("[CommunicationService] SMS technical dispatch SUCCESS for {To}", request.To);
                }
                else
                {
                    _logger.LogWarning("[CommunicationService] SMS technical dispatch FAILED for {To}", request.To);
                    warningMsg = "Warning: The SMS service (Twilio) failed to deliver the message.";
                }
            }

            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                PartyId = request.PartyId,
                ChannelId = channel.ChannelId,
                AdjusterId = adjusterId,
                Direction = "Outgoing",
                MessageBody = body,
                MessageType = request.Mode,
                Status = isSent ? "Sent" : (isChannelEnabled ? "Failed" : "Disabled"),
                SentAt = isSent ? DateTime.UtcNow : DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = true,
                ReadAtDate = DateTime.UtcNow,
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync();
            assignedCommunicationId = communication.CommunicationId;
        }
        else
        {
            _logger.LogInformation("[CommunicationService] Storing {Mode} communication in database (Transmission disabled or not implemented)", request.Mode);
            // Standard Database Persistence for WhatsApp / etc.
            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                PartyId = request.PartyId,
                ChannelId = channel.ChannelId,
                AdjusterId = adjusterId,
                Direction = "Outgoing",
                MessageBody = body,
                MessageType = request.Mode,
                Status = isChannelEnabled ? "Sent" : "Failed",
                SentAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = true, // Outgoing messages are always auto-read
                ReadAtDate = DateTime.UtcNow,
            };

            try
            {
                _context.Communications.Add(communication);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                System.IO.File.WriteAllText(@"C:\Users\Admin\source\repos\CommunicationHub\db_error.log", innerMsg);
                throw;
            }

            assignedCommunicationId = communication.CommunicationId;
        }
        // Store attachments if provided
        if (request.AttachmentUrls != null && request.AttachmentUrls.Any())
        {
            var attachments = request.AttachmentUrls.Select(url => new MessageAttachment
            {
                AttachmentId = Guid.NewGuid(),
                CommunicationId = assignedCommunicationId,
                FileUrl = url,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.MessageAttachments.AddRange(attachments);
            await _context.SaveChangesAsync();
        }

        return (assignedCommunicationId, warningMsg);
    }

    public async Task<bool> ProcessIncomingSmsAsync(string fromNumber, string body, string messageSid)
    {
        try
        {
            _logger.LogInformation("[Webhook] Received incoming SMS. From: {From}, Body: {Body}, SID: {Sid}", 
                fromNumber, body, messageSid);

            var normalizedFrom = NormalizePhoneNumber(fromNumber);
            _logger.LogInformation("[Webhook] Normalized From: {Normalized}", normalizedFrom);

            // 1. Try to find the party by phone number
            // We search for both the raw and normalized version to be safe
            var party = await _context.InvolvedParties
                .Include(p => p.Claim)
                .ThenInclude(c => c!.ClaimAdjuster)
                .Where(p => p.Phone == fromNumber || p.Phone == normalizedFrom)
                .OrderByDescending(p => p.PartyId) // If multiple, pick the latest one
                .FirstOrDefaultAsync();

            int? claimId = party?.ClaimId;
            int? partyId = party?.PartyId;
            int? adjusterId = null;

            if (party != null)
            {
                _logger.LogInformation("[Webhook] Match found: PartyId={PartyId}, ClaimId={ClaimId}", 
                    partyId, claimId);
                
                // Try to get the primary adjuster assigned to this claim
                var assignment = party.Claim?.ClaimAdjuster;
                adjusterId = assignment?.AdjusterId;
            }
            else
            {
                _logger.LogWarning("[Webhook] No matching party found for phone: {From}", fromNumber);
            }

            // 2. Resolve Channel ID for SMS (usually 2)
            var smsChannel = await _context.Channels.FirstOrDefaultAsync(c => c.Name == "SMS");
            int channelId = smsChannel?.ChannelId ?? 2;

            // 3. Create the Communication record
            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                ClaimId = claimId,
                PartyId = partyId,
                AdjusterId = adjusterId, 
                ChannelId = channelId,
                Direction = "Incoming",
                MessageBody = body,
                MessageType = "SMS",
                Status = "Received",
                SentAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = false, // Incoming messages start as unread
                ReadAtDate = null
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[Webhook] SMS stored successfully. CommunicationId: {Id}", 
                communication.CommunicationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Webhook] Error processing incoming SMS from {From}", fromNumber);
            return false;
        }
    }

    public async Task<bool> ValidateAdjusterAccessAsync(int adjusterId, int claimId)
    {
        var assignment = await _context.ClaimAdjusters
            .FirstOrDefaultAsync(ca => ca.AdjusterId == adjusterId && ca.ClaimId == claimId);

        return assignment != null;
    }

    public async Task<Dictionary<string, bool>> GetEnabledChannelsAsync()
    {
        var channels = await _context.Channels.ToListAsync();
        return channels.ToDictionary(c => c.Name ?? "Unknown", c => c.IsActive.HasValue && c.IsActive.Value);
    }

    private string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Remove all non-numeric characters except the leading '+'
        var digits = new string(phone.Where(c => char.IsDigit(c)).ToArray());
        
        if (phone.StartsWith("+"))
            return "+" + digits;
            
        // If it starts with local prefix, you might want to force a country code here
        // For now, just return digits or keep it simple
        return digits;
    }

    private string TruncateMessage(string? message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length > maxLength ? message.Substring(0, maxLength) + "..." : message;
    }
}
