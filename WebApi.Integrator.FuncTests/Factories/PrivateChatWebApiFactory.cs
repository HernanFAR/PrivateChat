﻿using Core.UseCases.CreateUser;
using CrossCutting;
using CrossCutting.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace WebApi.Integrator.FuncTests.Factories;

public class PrivateChatWebApiFactory : WebApplicationFactory<Program>
{
    public UserManager UserManagerInstance { get; } = new UserManager();

    public PrivateChatWebApiFactory()
    {
        ClientOptions.AllowAutoRedirect = false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());

        builder.ConfigureTestServices(e =>
        {
            e.RemoveAll<UserManager>();
            e.AddSingleton(UserManagerInstance);
        });

        // Configure the server address for the server to
        // listen on for HTTPS requests on a dynamic port.
        //builder.UseUrls(_baseUrl.ToString());
    }

    public async Task<(string Token, string UserId)> GenerateJwtTokenForName(string name, TimeSpan? customDuration = null)
    {
        var httpClient = CreateClient();
        var contract = new CreateUserContract(name);

        var httpResponse = await httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract);

        var createUserResponse = await httpResponse.Content.ReadFromJsonAsync<CreateUserCommandResponse>();

        var tokenHandler = new JwtSecurityTokenHandler();

        var jsonToken = tokenHandler.ReadJwtToken(createUserResponse!.Token);
        var userId = jsonToken.Claims.First(e => e.Type == ClaimTypes.NameIdentifier).Value;

        if (customDuration is null) return (createUserResponse!.Token, userId);

        var jwtConfigMonitor = Services.GetRequiredService<IOptions<JwtConfiguration>>();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigMonitor.Value.IssuerSigningKey));
        var claims = new Claim[]
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.NameIdentifier, userId),
        };

        var jwt = new JwtSecurityToken(
            issuer: jwtConfigMonitor.Value.Issuer,
            audience: jwtConfigMonitor.Value.Audience,
            claims: claims,
            expires: DateTime.Now.Add(customDuration.Value),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (tokenHandler.WriteToken(jwt), userId);

    }

    public HubConnection CreateChatHubConnection(string jwt)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(ClientOptions.BaseAddress, ChatHub.Url), opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult(jwt)!;
                opts.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
    }
}