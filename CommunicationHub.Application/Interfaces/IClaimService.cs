using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;

namespace CommunicationHub.Application.Interfaces;

public interface IClaimService
{
    /// <summary>
    /// Gets all claims assigned to the logged-in adjuster
    /// </summary>
    Task<List<AssignedClaimDto>> GetAssignedClaimsAsync(int adjusterId);

    /// <summary>
    /// Gets the claim details with involved parties
    /// </summary>
    Task<ClaimDetailsDto> GetClaimDetailsAsync(int claimId);

    /// <summary>
    /// Gets involved parties for a specific claim
    /// </summary>
    Task<List<InvolvedPartyDto>> GetInvolvedPartiesAsync(int claimId);
}
