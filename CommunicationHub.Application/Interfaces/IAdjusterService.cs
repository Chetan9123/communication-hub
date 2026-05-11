using System;
using System.Threading.Tasks;
using CommunicationHub.API.DTOs;

namespace CommunicationHub.Application.Interfaces;

public interface IAdjusterService
{
    /// <summary>
    /// Gets the adjuster dashboard information
    /// </summary>
    Task<AdjusterDashboardDto> GetDashboardAsync(int adjusterId);
    Task<bool> ToggleStatusAsync(int adjusterId);
}
