using Core.UseCases.EnterRoom;
using Core.UseCases.SendMessage;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class SendMessageTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public SendMessageTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SendMessage_ShouldShowUnprocessableEntity_DetailUserNotConnectedToHub()
    {
        const string roomName = "2";
        var roomUrl = SendMessageEndpoint.Url
            .Replace("{room}", roomName);

        var contract = new SendMessageContract("Testing");

        var (jwt, _) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var response = await httpClient.PostAsJsonAsync(roomUrl, contract);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(SendMessageValidator.UserNotRegisteredInUserManager);
    }

    [Fact]
    public async Task SendMessage_ShouldShowUnprocessableEntity_DetailMessageNotValid()
    {
        const string roomName = "2";
        var roomUrl = SendMessageEndpoint.Url
            .Replace("{room}", roomName);

        var contract = new SendMessageContract("");

        var (jwt, _) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var response = await httpClient.PostAsJsonAsync(roomUrl, contract);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(SendMessageValidator.MessageNotEmptyMessage);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnNotFound()
    {
        const string roomName = "1";
        var roomUrl = SendMessageEndpoint.Url
            .Replace("{room}", roomName);

        var contract = new SendMessageContract("Testing");

        var (jwt, _) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);
        await hub.StartAsync();

        var response = await httpClient.PostAsJsonAsync(roomUrl, contract);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }

    [Fact]
    public async Task SendMessage_ShouldReceiveMessage_DetailCustomMessage()
    {
        var messageSend = false;
        const string roomName1 = "1";
        const string roomName2 = "2";
        var room1EnterUrl = EnterRoomEndpoint.Url
            .Replace("{room}", roomName1);
        var room2EnterUrl = EnterRoomEndpoint.Url
            .Replace("{room}", roomName2);
        var room1MessageUrl = SendMessageEndpoint.Url
            .Replace("{room}", roomName1);
        const string userName = "Fabian";

        var contract = new SendMessageContract("Testing");

        var (jwtHernan, _) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");
        var (jwtFabian, userIdFabian) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Fabian");

        var httpClientHernan = _fixture.PrivateChatWebApi.CreateClient();
        httpClientHernan.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtHernan}");

        var httpClientFabian = _fixture.PrivateChatWebApi.CreateClient();
        httpClientFabian.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtFabian}");

        await using var hubHernan = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtHernan);
        await using var hubFabian = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtFabian);

        await hubFabian.StartAsync();
        await hubHernan.StartAsync();

        var response1 = await httpClientHernan.PostAsJsonAsync(room1EnterUrl, new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClientHernan.PostAsJsonAsync(room2EnterUrl, new object());
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var response3 = await httpClientFabian.PostAsJsonAsync(room1EnterUrl, new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        hubHernan.On<string, string, string, string, DateTimeOffset>("ReceiveMessage", (fromUser, fromUserId, roomId, message, datetime) =>
        {
            fromUser.Should().Be(userName);
            fromUserId.Should().Be(userIdFabian);
            roomId.Should().Be(roomName1);
            message.Should().Be(contract.Message);
            datetime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

            messageSend = true;
        });

        var response4 = await httpClientFabian.PostAsJsonAsync(room1MessageUrl, contract);
        response4.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(3000);

        messageSend.Should().BeTrue();
    }
}
