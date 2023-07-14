using Core.UseCases.EnterRoom;
using CrossCutting;
using Domain;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class EnterRoomTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public EnterRoomTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnterRoom_ShouldHaveRegisterRoomAtUserManager()
    {
        const string roomName = "2";
        var url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName);

        var (jwt, userId) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response = await httpClient.PostAsJsonAsync(url, new object());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.GetRoomsOfUser(userId).SuccessValue.Should().Contain(roomName);
    }

    [Fact]
    public async Task EnterRoom_ShouldShowUnprocessableEntity_DetailUserNotConnectedToHub()
    {
        const string roomName = "2";
        var url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName);

        var (jwt, _) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var response = await httpClient.PostAsJsonAsync(url, new object());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(EnterRoomValidator.UserNotRegisteredInUserManager);
    }

    [Fact]
    public async Task EnterRoom_ShouldShowUnprocessableEntity_DetailUserConnectedTooMuchRooms()
    {
        const string roomName1 = "1";
        const string roomName2 = "2";
        const string roomName3 = "3";
        const string roomName4 = "4";
        const string roomName5 = "5";
        const string roomName6 = "6";

        var room1Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName1);
        var room2Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName2);
        var room3Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName3);
        var room4Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName4);
        var room5Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName5);
        var room6Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName6);

        var (jwt, _) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response1 = await httpClient.PostAsJsonAsync(room1Url, new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClient.PostAsJsonAsync(room2Url, new object());
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var response3 = await httpClient.PostAsJsonAsync(room3Url, new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        var response4 = await httpClient.PostAsJsonAsync(room4Url, new object());
        response4.StatusCode.Should().Be(HttpStatusCode.OK);

        var response5 = await httpClient.PostAsJsonAsync(room5Url, new object());
        response5.StatusCode.Should().Be(HttpStatusCode.OK);

        var response6 = await httpClient.PostAsJsonAsync(room6Url, new object());
        response6.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response6.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(UserInfo.CantAddMoreRooms);
    }

    [Fact]
    public async Task EnterRoom_ShouldReceiveMessage_DetailWelcomeMessage()
    {
        var messageSend = false;
        const string roomName1 = "1";
        const string roomName2 = "2";

        var room1Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName1);
        var room2Url = EnterRoomEndpoint.Url
            .Replace("{room}", roomName2);

        const string userName = "Fabian";

        var (jwtHernan, _) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");
        var (jwtFabian, userIdFabian) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Fabian");

        var httpClientHernan = _fixture.PrivateChatWebApi.CreateClient();
        httpClientHernan.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtHernan}");

        var httpClientFabian = _fixture.PrivateChatWebApi.CreateClient();
        httpClientFabian.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtFabian}");

        await using var hubHernan = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtHernan);
        await using var hubFabian = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtFabian);

        await hubFabian.StartAsync();
        await hubHernan.StartAsync();

        var response1 = await httpClientHernan.PostAsJsonAsync(room1Url, new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClientHernan.PostAsJsonAsync(room2Url, new object());
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        hubHernan.On<string, string, string, string, DateTimeOffset>("ReceiveMessage", (fromUser, fromUserId, roomId, message, datetime) =>
        {
            fromUser.Should().Be(UserInfo.System.Name);
            fromUserId.Should().Be(UserInfo.System.Id);
            roomId.Should().Be(roomName1);
            message.Should().Be(string.Format(EnterRoomHandler.SystemWelcomeMessage, userName, userIdFabian));
            datetime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

            messageSend = true;
        });

        var response3 = await httpClientFabian.PostAsJsonAsync(room1Url, new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(3000);

        messageSend.Should().BeTrue();
    }
}
