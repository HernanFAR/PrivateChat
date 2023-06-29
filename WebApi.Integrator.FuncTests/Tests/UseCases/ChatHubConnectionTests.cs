using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using CrossCutting;
using FluentAssertions;
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
        var jwt = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();
    }

    [Fact]
    public async Task ConnectToHub_ShouldDisconnect_Detail_InvalidToken()
    {
        var jwt = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán", TimeSpan.FromSeconds(3));
        
        var httpClient = _fixture.PrivateChatWebApi.CreateClient();

        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using (var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt))
        {
            await hub.StartAsync();

            await Task.Delay(10000);

            hub.Closed += async exception =>
            {
                exception.Should().BeNull();
            };

            var response = await httpClient.PostAsync("/chat/new_room", new StringContent("", MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

    }
}