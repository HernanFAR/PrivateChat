using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Core.Extensions;

internal static class HttpContextExtensions
{
    public static string? TryGetClaim(this HttpContext? @this, string claimName)
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

    public static string? TryGetName(this HttpContext? @this)
    {
        return @this.TryGetClaim(ClaimTypes.Name) ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string GetName(this HttpContext? @this)
    {
        return @this.TryGetName() ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string? TryGetNameIdentifier(this HttpContext? @this)
    {
        return @this.TryGetClaim(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }

    public static string GetNameIdentifier(this HttpContext? @this)
    {
        return @this.TryGetNameIdentifier() ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }
}