using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using PrivateChat.CrossCutting.ChatWebApi;

namespace PrivateChat.Core.Abstractions;

public interface IApplicationLoginProvider
{
    string JwtKey { get; }

    Task Login(string token, CancellationToken cancellationToken = default);

    Task Logout(CancellationToken cancellationToken = default);
}

public class LoginStateProvider : AuthenticationStateProvider, IApplicationLoginProvider
{
    private readonly ISessionStorage _sessionStorage;
    private readonly HttpClient _httpClient;
    private readonly ChatWebApiConnection.ChatHub _chatHub;
    private static readonly string JwtKey_ = "JWT";

    public LoginStateProvider(ISessionStorage sessionStorage,
        HttpClient httpClient, ChatWebApiConnection.ChatHub chatHub)
    {
        _sessionStorage = sessionStorage;
        _httpClient = httpClient;
        _chatHub = chatHub;
    }

    public static readonly AuthenticationState AnonymousUser = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var jwt = await _sessionStorage.GetItemAsync<string?>(JwtKey);

        return string.IsNullOrEmpty(jwt) ? AnonymousUser : BuildFromJwt(jwt);
    }

    private AuthenticationState BuildFromJwt(string jwt)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var jwtHandler = new JwtSecurityTokenHandler();
        var tokenInfo = jwtHandler.ReadJwtToken(jwt);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(tokenInfo.Claims, "Bearer Jwt")));
    }

    public string JwtKey => JwtKey_;

    public async Task Login(string token, CancellationToken cancellationToken = default)
    {
        await _sessionStorage.SetItemAsync(JwtKey, token, cancellationToken);

        NotifyAuthenticationStateChanged(Task.FromResult(BuildFromJwt(token)));
    }

    public async Task Logout(CancellationToken cancellationToken = default)
    {
        await _sessionStorage.RemoveItemAsync(JwtKey, cancellationToken);
        await _chatHub.DisposeIfConnectedAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousUser));
    }
}
