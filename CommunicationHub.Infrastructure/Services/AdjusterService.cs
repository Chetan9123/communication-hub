using System;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunicationHub.Infrastructure.Services;

public class AdjusterService : IAdjusterService
{
    private readonly CommunicationHubDbContext _context;
    private readonly ICommunicationService _communicationService;

    public AdjusterService(CommunicationHubDbContext context, ICommunicationService communicationService)
    {
        _context = context;
        _communicationService = communicationService;
    }

    public async Task<AdjusterDashboardDto> GetDashboardAsync(int adjusterId)
    {
        try
        {
            var adjuster = await _context.Adjusters.FirstOrDefaultAsync(a => a.AdjusterId == adjusterId);

            if (adjuster == null)
                return new AdjusterDashboardDto();

            // Get claims assigned to this adjuster
            var claimAdjusters = await _context.ClaimAdjusters
                .Where(ca => ca.AdjusterId == adjusterId && ca.UnassignedAt == null)
                .Include(ca => ca.Claim)
                .ToListAsync();

            var unreadCount = 0;
            var assignedClaims = new System.Collections.Generic.List<AssignedClaimDto>();

            foreach (var claimAdjuster in claimAdjusters)
            {
                var claim = claimAdjuster.Claim;
                if (claim != null)
                {
                    // Count unread communications for this claim
                    var unreadInClaim = await _context.Communications
                        .CountAsync(c => c.ClaimId == claim.ClaimId && !c.ReadAt.HasValue);

                    unreadCount += unreadInClaim;

                    assignedClaims.Add(new AssignedClaimDto
                    {
                        ClaimId = claim.ClaimId,
                        ClaimNumber = claim.ClaimNumber,
                        PolicyNumber = claim.PolicyNumber,
                        Status = claim.Status,
                        ClaimFiledOn = claim.ClaimFiledOn,
                        UnreadCommunicationCount = unreadInClaim
                    });
                }
            }

            return new AdjusterDashboardDto
            {
                AdjusterId = adjuster.AdjusterId,
                AdjusterName = adjuster.FullName,
                Email = adjuster.Email,
                UnreadCommunicationCount = unreadCount,
                AssignedClaims = assignedClaims
            };
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error in GetDashboardAsync: {ex.Message}");
            throw;
        }
    }
}
