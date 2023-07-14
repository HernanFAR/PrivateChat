using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Core.UseCases.EnterRoom;
using CrossCutting;
using Microsoft.Extensions.DependencyInjection;
using Core.UseCases.CreateUser;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class ChatHubConnectionTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public ChatHubConnectionTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConnectToHub_ShouldConnectCorrectly()
    {
        var (jwt, userId) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.Get(userId).SuccessValue.Should().NotBeNull();
    }

    [Fact]
    public async Task ConnectToHub_ShouldConnectCorrectly_DetailAfterDisconnection()
    {
        var (jwt, userId) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();

        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        await using (var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt))
        {
            await hub.StartAsync();

            userManager.Get(userId).SuccessValue.Should().NotBeNull();
        }

        await Task.Delay(6000);
        userManager.Get(userId).IsFailure.Should().BeTrue();

        await using (var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt))
        {
            await hub.StartAsync();

            userManager.Get(userId).SuccessValue.Should().NotBeNull();
        }
    }
}