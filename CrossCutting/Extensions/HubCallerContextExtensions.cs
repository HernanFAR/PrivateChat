using System.Security.Claims;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.SignalR;

internal static class HubCallerContextExtensions
{
    public static string? TryGetClaim(this HubCallerContext? @this, string claimName)
    {
        if (@this?.User is null)
        {
            throw new InvalidOperationException(nameof(@this));
        }

        if (@this.User.Identity is not ClaimsIdentity identity)
        {
            throw new InvalidOperationException(nameof(identity));
        }

        var nameClaim = identity.FindFirst(claimName);

        return nameClaim?.Value;
    }

    public static string? TryGetName(this HubCallerContext? @this)
    {
        return @this.TryGetClaim(ClaimTypes.Name) ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string GetName(this HubCallerContext? @this)
    {
        return @this.TryGetName() ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string? TryGetNameIdentifier(this HubCallerContext? @this)
    {
        return @this.TryGetClaim(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }

    public static string GetNameIdentifier(this HubCallerContext? @this)
    {
        return @this.TryGetNameIdentifier() ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }
}