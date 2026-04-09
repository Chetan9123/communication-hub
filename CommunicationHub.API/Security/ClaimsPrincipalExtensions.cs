using System.Security.Claims;

namespace CommunicationHub.API.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAdjusterId(this ClaimsPrincipal user, out int adjusterId)
    {
        adjusterId = default;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var claimValue = user.FindFirstValue("AdjusterId") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out adjusterId);
    }
}

