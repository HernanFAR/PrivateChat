// ReSharper disable once CheckNamespace
namespace System.Security.Claims;

internal static class ClaimsPrincipalExtensions
{
    public static string GetNameIdentifier(this ClaimsPrincipal? identity)
    {
        if (identity is null)
        {
            throw new InvalidOperationException(nameof(identity));
        }

        return identity.FindFirst(ClaimTypes.NameIdentifier)
            ?.Value
            ?? throw new InvalidOperationException(nameof(ClaimTypes.NameIdentifier));
    }
}
