using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PrivateChat.CrossCutting.Abstractions;

public interface IApplicationLoginProvider
{
    Task Login(string token, CancellationToken cancellationToken = default);

    Task Logout(CancellationToken cancellationToken = default);
}

public class LoginStateProvider : AuthenticationStateProvider, IApplicationLoginProvider
{
    private readonly IApplicationStorage _applicationStorage;
    private readonly HttpClient _httpClient;
    public const string JwtKey = "JWT";

    public LoginStateProvider(IApplicationStorage applicationStorage,
        HttpClient httpClient)
    {
        _applicationStorage = applicationStorage;
        _httpClient = httpClient;
    }

    public static readonly AuthenticationState AnonymousUser = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var jwt = await _applicationStorage.GetItemAsync<string?>(JwtKey);

        return string.IsNullOrEmpty(jwt) ? AnonymousUser : BuildFromJwt(jwt);
    }

    private AuthenticationState BuildFromJwt(string jwt)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var jwtHandler = new JwtSecurityTokenHandler();
        var tokenInfo = jwtHandler.ReadJwtToken(jwt);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(tokenInfo.Claims, "Bearer Jwt")));
    }

    public async Task Login(string token, CancellationToken cancellationToken = default)
    {
        await _applicationStorage.SetItemAsync(JwtKey, token, cancellationToken);

        NotifyAuthenticationStateChanged(Task.FromResult(BuildFromJwt(token)));
    }

    public async Task Logout(CancellationToken cancellationToken = default)
    {
        await _applicationStorage.RemoveItemAsync(JwtKey, cancellationToken);

        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousUser));
    }
}
