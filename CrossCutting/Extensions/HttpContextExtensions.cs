using System.Security.Claims;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    public static string? TryGetClaim(this HttpContext? @this, string claimName)
    {
        if (@this?.User is null)
        {
            throw new InvalidOperationException(nameof(@this));
        }

        return @this.User.TryGetClaim(claimName);
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