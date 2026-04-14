using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunicationHub.Infrastructure.Services;

public class ClaimService : IClaimService
{
    private readonly CommunicationHubDbContext _context;

    public ClaimService(CommunicationHubDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssignedClaimDto>> GetAssignedClaimsAsync(int adjusterId)
    {
        var assignedClaims = await _context.ClaimAdjusters
            .Where(ca => ca.AdjusterId == adjusterId)
            .Include(ca => ca.Claim)
            .ThenInclude(c => c!.Communications)
            .Select(ca => new AssignedClaimDto
            {
                ClaimId = ca.Claim!.ClaimId,
                ClaimNumber = ca.Claim.ClaimNumber,
                PolicyNumber = ca.Claim.PolicyNumber,
                Status = ca.Claim.Status,
                ClaimFiledOn = ca.Claim.ClaimFiledOn,
                UnreadCommunicationCount = ca.Claim.Communications
                    .Count(c => !c.ReadAt.HasValue && c.IsActive.HasValue && c.IsActive.Value)
            })
            .ToListAsync();

        return assignedClaims;
    }

    public async Task<ClaimDetailsDto> GetClaimDetailsAsync(int claimId)
    {
        var claim = await _context.Claims
            .Include(c => c.InvolvedParties)
            .Include(c => c.ClaimAdjuster)
            .ThenInclude(ca => ca!.Adjuster)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return new ClaimDetailsDto();

        return new ClaimDetailsDto
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            PolicyNumber = claim.PolicyNumber,
            ClaimFiledOn = claim.ClaimFiledOn,
            ClaimClosedOn = claim.ClaimClosedOn,
            Status = claim.Status,
            AssignedAdjusterName = claim.ClaimAdjuster?.Adjuster?.FullName,
            IsAdjusterActive = claim.ClaimAdjuster?.Adjuster?.IsActive,
            InvolvedParties = claim.InvolvedParties
                .Where(p => p.IsActive.HasValue && p.IsActive.Value)
                .Select(p => new InvolvedPartyDto
                {
                    PartyId = p.PartyId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Phone = p.Phone,
                    Email = p.Email,
                    InvolvedPartyType = p.InvolvedPartyType,
                    IsActive = p.IsActive
                })
                .ToList()
        };
    }

    public async Task<List<InvolvedPartyDto>> GetInvolvedPartiesAsync(int claimId)
    {
        var parties = await _context.InvolvedParties
            .Where(p => p.ClaimId == claimId && p.IsActive.HasValue && p.IsActive.Value)
            .Select(p => new InvolvedPartyDto
            {
                PartyId = p.PartyId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Phone = p.Phone,
                Email = p.Email,
                InvolvedPartyType = p.InvolvedPartyType,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return parties;
    }
}
