using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.Infrastructure.Hubs;
using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using CommunicationHub.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace CommunicationHub.Infrastructure.Services;

public class CommunicationService : ICommunicationService
{
    private readonly CommunicationHubDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IStorageService _storageService;
    private readonly IS3Service _s3Service;
    private readonly IHubContext<MessagingHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IAutoReplyService _autoReplyService;
    private readonly ILogger<CommunicationService> _logger;

    public CommunicationService(
        CommunicationHubDbContext context, 
        IEmailService emailService, 
        ISmsService smsService,
        IWhatsAppService whatsAppService,
        IStorageService storageService,
        IS3Service s3Service,
        IHubContext<MessagingHub> hubContext,
        IConfiguration configuration,
        IAutoReplyService autoReplyService,
        ILogger<CommunicationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _whatsAppService = whatsAppService;
        _storageService = storageService;
        _s3Service = s3Service;
        _hubContext = hubContext;
        _configuration = configuration;
        _autoReplyService = autoReplyService;
        _logger = logger;
    }

    public async Task<List<UnreadCommunicationDto>> GetUnreadCommunicationsAsync(int adjusterId)
    {
        var unreadMessages = await _context.Communications
            .Where(c => c.Adjuster!.AdjusterId == adjusterId && (c.ReadAt == null || c.ReadAt == false) && c.IsActive.HasValue && c.IsActive.Value)
            .Include(c => c.Claim)
            .Include(c => c.Party)
            .Include(c => c.Channel)
            .Include(c => c.MessageAttachments)
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
            Status = m.Status,
            SenderPhone = m.Party?.Phone,
            SenderEmail = m.Party?.Email,
            Attachments = m.MessageAttachments.Select(a => new AttachmentDto
            {
                AttachmentId = a.AttachmentId,
                FileUrl = a.FileUrl,
                MimeType = a.MimeType,
                FileSize = a.FileSize,
                FileName = a.FileName
            }).ToList()
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
                Notes = m.Notes,
                PartyName = $"{party.FirstName} {party.LastName}".Trim(),
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

    public async Task<CommunicationThreadDto> GetClaimCommunicationThreadAsync(int claimId)
    {
        var claim = await _context.Claims
            .Include(c => c.InvolvedParties)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return new CommunicationThreadDto();

        var messages = await _context.Communications
            .Where(c => c.ClaimId == claimId && c.IsActive.HasValue && c.IsActive.Value)
            .Include(c => c.Party)
            .Include(c => c.MessageAttachments)
            .Include(c => c.Channel)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return new CommunicationThreadDto
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            PolicyNumber = claim.PolicyNumber,
            PartyId = 0, // Unused in this context
            PartyName = "All Parties",
            Messages = messages.Select(m => new CommunicationMessageDto
            {
                CommunicationId = m.CommunicationId,
                Direction = m.Direction,
                Timestamp = m.SentAt ?? m.ReceivedAt ?? m.CreatedAt,
                Mode = m.Channel?.Name,
                MessageBody = m.MessageBody,
                Status = m.Status,
                IsRead = m.ReadAt.HasValue && m.ReadAt.Value,
                Notes = m.Notes,
                PartyName = m.Party != null ? $"{m.Party.FirstName} {m.Party.LastName}".Trim() : "Unknown",
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
        var communication = await _context.Communications.FindAsync(communicationId);
        if (communication == null)
            return false;

        communication.Notes = notes;
        _context.Communications.Update(communication);
        await _context.SaveChangesAsync();
        return true;
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

        // --- Handle Outgoing Attachments ---
        var outboundAttachments = new List<MessageAttachment>();
        if (request.AttachmentIds != null && request.AttachmentIds.Any())
        {
            outboundAttachments = await _context.MessageAttachments
                .Where(a => request.AttachmentIds.Contains(a.AttachmentId))
                .ToListAsync();
        }

        // If the mode is Email, delegate entirely to our specialized IEmailService
        // (It already handles the database logging)
        if (request.Mode == "Email")
        {
            // Prepare attachments for Email
            var emailAttachments = new List<(string FileName, Stream Data, string ContentType)>();

            try
            {
                // Process explicitly mapped database attachments
                foreach (var att in outboundAttachments)
                {
                    if (!string.IsNullOrEmpty(att.S3Key))
                    {
                        try 
                        {
                            if (att.FileSize > 25 * 1024 * 1024)
                            {
                                _logger.LogWarning("Attachment {FileName} exceeds 25MB limit. Skipping.", att.FileName);
                                continue;
                            }

                            var stream = await _s3Service.GetFileStreamAsync(att.S3Key);
                            emailAttachments.Add((att.FileName ?? "attachment", stream, att.MimeType ?? "application/octet-stream"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to download S3 attachment {S3Key} for email.", att.S3Key);
                        }
                    }
                }

                // Process URLs explicitly provided by the UI (if any)
                if (request.AttachmentUrls != null)
                {
                    var httpClient = new HttpClient();
                    int idx = 1;
                    foreach (var url in request.AttachmentUrls)
                    {
                        try 
                        {
                            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode)
                            {
                                if (response.Content.Headers.ContentLength > 25 * 1024 * 1024)
                                {
                                    _logger.LogWarning("Attachment URL {Url} exceeds 25MB limit. Skipping.", url);
                                    continue;
                                }

                                var stream = await response.Content.ReadAsStreamAsync();
                                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                                
                                string fileName = $"attachment_{idx}";
                                try {
                                    var uri = new Uri(url);
                                    var name = Path.GetFileName(uri.AbsolutePath);
                                    if (!string.IsNullOrEmpty(name)) fileName = name;
                                } catch { }

                                emailAttachments.Add((fileName, stream, contentType));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to download attachment from URL {Url}", url);
                        }
                        idx++;
                    }
                }

                var result = await _emailService.SendEmailAsync(
                    request.ClaimId,
                    request.PartyId,
                    request.To,
                    request.Subject ?? "Communication Update", // fallback subject
                    body,
                    adjusterId,
                    emailAttachments);

                assignedCommunicationId = result.CommunicationId;
                if (!result.Sent && isChannelEnabled)
                    warningMsg = "Warning: The email was saved to the database, but it could not be sent via the SMTP service. It remains stored for tracking.";
            }
            finally
            {
                // Ensure all open streams are disposed immediately after the email sending returns
                foreach(var (fileName, stream, contentType) in emailAttachments)
                {
                    stream?.Dispose();
                }
            }
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
                MessageType = request.Mode,
                MessageBody = body,
                Status = isSent ? "Sent" : (isChannelEnabled ? "Failed" : "Disabled"),
                SentAt = isSent ? DateTime.UtcNow : DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = true,
                ReadAtDate = DateTime.UtcNow,
            };

            _context.Communications.Add(communication);
            
            // Link attachments to this communication
            foreach (var att in outboundAttachments)
            {
                att.CommunicationId = communication.CommunicationId;
            }
            
            await _context.SaveChangesAsync();
            assignedCommunicationId = communication.CommunicationId;
        }
        else if (request.Mode == "WhatsApp")
        {
            _logger.LogInformation("[CommunicationService] Attempting to send WhatsApp to {To} for ClaimId {ClaimId}", request.To, request.ClaimId);
            
            WhatsAppSendResult sendResult = new WhatsAppSendResult { Success = false };
            if (isChannelEnabled)
            {
                // Status callback URL: /api/webhooks/whatsapp/status
                var baseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5192";
                var statusCallback = $"{baseUrl}/api/webhooks/whatsapp/status";

                // Pre-signed URLs for WhatsApp attachments
                var mediaUrls = new List<string>();
                if (request.AttachmentUrls != null) mediaUrls.AddRange(request.AttachmentUrls);
                
                foreach (var att in outboundAttachments)
                {
                    if (!string.IsNullOrEmpty(att.S3Key))
                    {
                        var preSignedUrl = await _s3Service.GeneratePreSignedUrlAsync(att.S3Key, 60);
                        mediaUrls.Add(preSignedUrl);
                    }
                }

                sendResult = await _whatsAppService.SendWhatsAppAsync(request.To, body, mediaUrls, statusCallback);
                
                if (!sendResult.Success)
                {
                    _logger.LogWarning("[CommunicationService] WhatsApp technical dispatch FAILED for {To}. Error: {Error}", request.To, sendResult.ErrorMessage);
                    warningMsg = $"Warning: The WhatsApp service (Twilio) failed to deliver the message. {sendResult.ErrorMessage}";
                }
            }

            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                Sid = sendResult.Sid,
                ClaimId = request.ClaimId,
                PartyId = request.PartyId,
                ChannelId = channel.ChannelId,
                AdjusterId = adjusterId,
                Direction = "Outgoing",
                MessageBody = body,
                MessageType = request.Mode,
                Status = sendResult.Success ? "Sent" : (isChannelEnabled ? "Failed" : "Disabled"),
                SentAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = true,
                ReadAtDate = DateTime.UtcNow,
            };

            _context.Communications.Add(communication);
            
            // Link attachments to this communication
            foreach (var att in outboundAttachments)
            {
                att.CommunicationId = communication.CommunicationId;
            }
            
            await _context.SaveChangesAsync();
            assignedCommunicationId = communication.CommunicationId;

            // Notify real-time clients in the claim group
            await _hubContext.Clients.Group(request.ClaimId.ToString()).SendAsync("ReceiveCommunication", new
            {
                mId = communication.CommunicationId,
                id = communication.ClaimId,
                pId = communication.PartyId,
                dir = communication.Direction,
                txt = communication.MessageBody,
                stat = communication.Status,
                ts = communication.SentAt
            });
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

                // Link attachments to this communication
                foreach (var att in outboundAttachments)
                {
                    att.CommunicationId = communication.CommunicationId;
                }

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
        return await ProcessIncomingGenericAsync(fromNumber, body, messageSid, "SMS", null);
    }

    public async Task<bool> ProcessIncomingWhatsAppAsync(string fromNumber, string body, string messageSid, List<string>? mediaUrls = null)
    {
        return await ProcessIncomingGenericAsync(fromNumber, body, messageSid, "WhatsApp", mediaUrls);
    }

    private async Task<bool> ProcessIncomingGenericAsync(string fromNumber, string body, string messageSid, string mode, List<string>? mediaUrls)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromNumber))
            {
                _logger.LogWarning("[Webhook] Aborting {Mode} processing: Missing fromNumber.", mode);
                return false;
            }

            _logger.LogInformation("[Webhook] Received incoming {Mode}. From: {From}, Body: {Body}, SID: {Sid}", 
                mode, fromNumber, body, messageSid);

            // Normalize phone number (handle nulls safely)
            var normalizedFrom = NormalizePhoneNumber(fromNumber);
            _logger.LogInformation("[Webhook] Normalized From: {Normalized}", normalizedFrom);

            // 1. Try to find the party by phone number
            var party = await _context.InvolvedParties
                .Include(p => p.Claim)
                .ThenInclude(c => c!.ClaimAdjuster)
                .Where(p => p.Phone == fromNumber || p.Phone == normalizedFrom || (mode == "WhatsApp" && p.Phone == $"whatsapp:{normalizedFrom}"))
                .OrderByDescending(p => p.PartyId)
                .FirstOrDefaultAsync();

            int? claimId = party?.ClaimId;
            int? partyId = party?.PartyId;
            int? adjusterId = null;

            if (party != null)
            {
                _logger.LogInformation("[Webhook] Match found: PartyId={PartyId}, ClaimId={ClaimId}", 
                    partyId, claimId);
                var assignment = party.Claim?.ClaimAdjuster;
                adjusterId = assignment?.AdjusterId;
            }

            // 2. Resolve Channel ID
            var channel = await _context.Channels.FirstOrDefaultAsync(c => c.Name == mode);
            int channelId = channel?.ChannelId ?? (mode == "SMS" ? 2 : 3);

            // 3. Create the Communication record
            var communication = new Communication
            {
                CommunicationId = Guid.NewGuid(),
                Sid = messageSid,
                ClaimId = claimId,
                PartyId = partyId,
                AdjusterId = adjusterId, 
                ChannelId = channelId,
                Direction = "Incoming",
                MessageBody = body,
                MessageType = mode,
                Status = "Received",
                SentAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ReadAt = false,
                ReadAtDate = null
            };

            _context.Communications.Add(communication);
            await _context.SaveChangesAsync();

            // 4. Handle Attachments (Incoming)
            if (mediaUrls != null && mediaUrls.Any())
            {
                var accountSid = _configuration["Twilio:AccountSid"]!;
                var authToken = _configuration["Twilio:AuthToken"]!;
                using var client = new HttpClient();
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                int count = 1;
                foreach (var url in mediaUrls)
                {
                    // Defensive try-catch around each attachment so one failure doesn't break the whole message
                    try
                    {
                        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("[Webhook] Failed to access attachment at {Url}. Status: {Status}", url, response.StatusCode);
                            continue;
                        }

                        // 1. Validation (Size)
                        var contentLength = response.Content.Headers.ContentLength;
                        if (contentLength.HasValue && contentLength.Value > 25 * 1024 * 1024) // 25MB limit
                        {
                            _logger.LogWarning("[Webhook] Attachment too large: {Size} bytes. Skipping.", contentLength.Value);
                            continue;
                        }

                        // 2. Validation (Type)
                        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                        // Broadened validation: allow images, videos, audio, and common docs
                        var allowedTypes = new[] { 
                            "image/jpeg", "image/png", "image/gif", "image/webp",
                            "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
                            "audio/mpeg", "audio/wav", "audio/ogg", "audio/aac",
                            "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "application/zip", "application/x-zip-compressed", "text/plain"
                        };

                        if (!allowedTypes.Contains(contentType) && !contentType.StartsWith("image/") && !contentType.StartsWith("video/"))
                        {
                            _logger.LogWarning("[Webhook] Unsupported attachment type: {Type}. Skipping.", contentType);
                            continue;
                        }

                        // 3. Download and Upload to S3
                        using var stream = await response.Content.ReadAsStreamAsync();
                        var extension = contentType.Split('/').LastOrDefault() ?? "bin";
                        if (extension == "jpeg") extension = "jpg";
                        var fileName = $"{messageSid}_{count}.{extension}";

                        var s3Key = await _s3Service.UploadFileAsync(stream, fileName, contentType, communication.CommunicationId);

                        // 4. Save Metadata
                        var attachment = new MessageAttachment
                        {
                            AttachmentId = Guid.NewGuid(),
                            CommunicationId = communication.CommunicationId,
                            FileName = fileName,
                            S3Key = s3Key,
                            MimeType = contentType,
                            FileType = extension,
                            FileSize = (int?)(contentLength ?? 0),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.MessageAttachments.Add(attachment);
                        count++;
                        _logger.LogInformation("[Webhook] Attachment saved to S3: {S3Key}", s3Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Webhook] Unexpected error processing attachment {Url}", url);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // 5. SignalR Notification
            if (claimId.HasValue)
            {
                await _hubContext.Clients.Group(claimId.Value.ToString()).SendAsync("ReceiveCommunication", new
                {
                    mId = communication.CommunicationId,
                    id = communication.ClaimId,
                    pId = communication.PartyId,
                    dir = communication.Direction,
                    txt = communication.MessageBody,
                    stat = communication.Status,
                    ts = communication.ReceivedAt
                });
            }
            
            // 6. Trigger Auto-Reply if Adjuster is Inactive
            if (claimId.HasValue && partyId.HasValue)
            {
                // Background execution to not block the webhook response
                _ = Task.Run(() => _autoReplyService.TriggerAutoReplyIfInactiveAsync(claimId.Value, partyId.Value, mode));
            }

            _logger.LogInformation("[Webhook] {Mode} stored and broadcasted. CommunicationId: {Id}", 
                mode, communication.CommunicationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Webhook] Error processing incoming {Mode} from {From}", mode, fromNumber);
            return false;
        }
    }

    public async Task<bool> UpdateCommunicationStatusBySidAsync(string sid, string status)
    {
        var comm = await _context.Communications.FirstOrDefaultAsync(c => c.Sid == sid);
        if (comm == null) return false;

        comm.Status = status;
        if (status.Equals("delivered", StringComparison.OrdinalIgnoreCase))
        {
            comm.DeliveredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Notify client about status change if claimId is known
        if (comm.ClaimId.HasValue)
        {
            await _hubContext.Clients.Group(comm.ClaimId.Value.ToString()).SendAsync("UpdateCommunicationStatus", new
            {
                mId = comm.CommunicationId,
                stat = status
            });
        }

        return true;
    }

    public async Task<int> SyncMissedTwilioMessagesAsync()
    {
        int processedCount = 0;
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var targetNumber = _configuration["Twilio:WhatsAppNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(targetNumber))
            {
                _logger.LogWarning("[Sync] Missing Twilio configuration. Cannot sync messages.");
                return 0;
            }

            TwilioClient.Init(accountSid, authToken);

            _logger.LogInformation("[Sync] Fetching Twilio WhatsApp messages for the last 24 hours...");

            var messages = await MessageResource.ReadAsync(
                to: new Twilio.Types.PhoneNumber(targetNumber),
                dateSentAfter: DateTime.UtcNow.AddHours(-24),
                limit: 100
            );

            foreach (var message in messages)
            {
                if (message.Direction != MessageResource.DirectionEnum.Inbound)
                    continue;

                bool exists = await _context.Communications.AnyAsync(c => c.Sid == message.Sid);
                if (exists)
                    continue;

                _logger.LogInformation("[Sync] Found missing message {Sid}. Processing now...", message.Sid);

                var mediaUrls = new List<string>();
                if (int.TryParse(message.NumMedia, out int numMedia) && numMedia > 0)
                {
                    var mediaList = await Twilio.Rest.Api.V2010.Account.Message.MediaResource.ReadAsync(pathMessageSid: message.Sid);
                    foreach (var media in mediaList)
                    {
                        var fullUrl = media.Uri.StartsWith("http") ? media.Uri : $"https://api.twilio.com{media.Uri}";
                        fullUrl = fullUrl.Replace(".json", "");
                        mediaUrls.Add(fullUrl);
                    }
                }

                bool success = await ProcessIncomingWhatsAppAsync(
                    fromNumber: message.From.ToString(),
                    body: message.Body,
                    messageSid: message.Sid,
                    mediaUrls: mediaUrls
                );

                if (success)
                    processedCount++;
            }

            _logger.LogInformation("[Sync] Successfully synced {Count} missed WhatsApp messages.", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sync] Error occurred while syncing missed Twilio messages.");
        }

        return processedCount;
    }


    public async Task<bool> ValidateAdjusterAccessAsync(int adjusterId, int claimId)
    {
        // Primary check: is the adjuster directly assigned via ClaimAdjusters?
        var directAssignment = await _context.ClaimAdjusters
            .AnyAsync(ca => ca.AdjusterId == adjusterId && ca.ClaimId == claimId);

        if (directAssignment) return true;

        // Fallback: has this adjuster already processed communications for this claim?
        // (e.g. they received the unread message that they are now trying to reply to)
        var hasComms = await _context.Communications
            .AnyAsync(c => c.AdjusterId == adjusterId && c.ClaimId == claimId);

        return hasComms;
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

        // Remove whatsapp: prefix if present
        var cleanPhone = phone.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase).Trim();

        // Remove all non-numeric characters except the leading '+'
        var digits = new string(cleanPhone.Where(c => char.IsDigit(c)).ToArray());
        
        if (cleanPhone.StartsWith("+"))
            return "+" + digits;
            
        return digits;
    }

    private string TruncateMessage(string? message, int maxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length > maxLength ? message.Substring(0, maxLength) + "..." : message;
    }
}
