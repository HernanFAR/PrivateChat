using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Core.UseCases.EnterRoom;
using CrossCutting;
using Microsoft.Extensions.DependencyInjection;

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
        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.GetUserOrDefault(userId).Should().NotBeNull();
    }

    [Fact]
    public async Task ConnectToHub_ShouldDisconnect_Detail_InvalidToken()
    {
        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán", TimeSpan.FromSeconds(3));

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();

        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using (var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt))
        {
            await hub.StartAsync();

            await Task.Delay(8500);

            hub.Closed += async exception =>
            {
                exception.Should().BeNull();
            };

            var url = EnterRoomEndpoint.Url
                .Replace("{room}", "new_room");

            var response = await httpClient.PostAsync(url, new StringContent("", MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await Task.Delay(2500);
        }

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.GetUserOrDefault(userId).Should().BeNull();
    }
}