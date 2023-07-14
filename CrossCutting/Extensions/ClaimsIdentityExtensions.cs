// ReSharper disable once CheckNamespace
namespace System.Security.Claims;

public static class ClaimsIdentityExtensions
{
    public static string? TryGetClaim(this ClaimsPrincipal? @this, string claimName)
    {
        if (@this is null)
        {
            throw new InvalidOperationException("identity");
        }

        var nameClaim = @this.FindFirst(claimName);

        return nameClaim?.Value;
    }
    
    public static string? TryGetName(this ClaimsPrincipal? @this)
    {
        return @this.TryGetClaim(ClaimTypes.Name) ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string GetName(this ClaimsPrincipal? @this)
    {
        return @this.TryGetName() ?? throw new InvalidOperationException(ClaimTypes.Name);
    }

    public static string? TryGetNameIdentifier(this ClaimsPrincipal? @this)
    {
        return @this.TryGetClaim(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }
    
    public static string GetNameIdentifier(this ClaimsPrincipal? @this)
    {
        return @this.TryGetNameIdentifier() ?? throw new InvalidOperationException(ClaimTypes.NameIdentifier);
    }
}