using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace CommunicationHub.Infrastructure.Hubs;
public class MessagingHub : Hub
{
    /// <summary>
    /// Join a group for a specific claim to receive real-time updates for that claim.
    /// </summary>
    public async Task JoinClaimGroup(int claimId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, claimId.ToString());
    }

    /// <summary>
    /// Leave a claim group.
    /// </summary>
    public async Task LeaveClaimGroup(int claimId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, claimId.ToString());
    }
}
