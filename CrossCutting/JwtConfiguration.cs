using System.Text;

namespace CrossCutting;

public class JwtConfiguration
{
    public string IssuerSigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }

    public byte[] IssuerSigningKeyBytes => Encoding.UTF8.GetBytes(IssuerSigningKey);
}