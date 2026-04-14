using System.Threading.Tasks;

namespace CommunicationHub.Application.Interfaces;

public interface IAutoReplyService
{
    /// <summary>
    /// Checks if the adjuster for a claim is inactive and triggers an automated reply if within cooldown.
    /// </summary>
    Task TriggerAutoReplyIfInactiveAsync(int claimId, int partyId, string channelName);
}
