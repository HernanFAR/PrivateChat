using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using CrossCutting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Core.UseCases.EnterRoom;
using Domain;
using Microsoft.AspNetCore.SignalR.Client;

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

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response = await httpClient.PostAsJsonAsync($"/chat/{roomName}", new object());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.GetRoomsOfUser(userId).Should().Contain(roomName);
    }

    [Fact]
    public async Task EnterRoom_ShouldShowUnprocessableEntity_DetailUserNotConnectedToHub()
    {
        const string roomName = "2";

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var response = await httpClient.PostAsJsonAsync($"/chat/{roomName}", new object());

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

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response1 = await httpClient.PostAsJsonAsync($"/chat/{roomName1}", new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClient.PostAsJsonAsync($"/chat/{roomName2}", new object());
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var response3 = await httpClient.PostAsJsonAsync($"/chat/{roomName3}", new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        var response4 = await httpClient.PostAsJsonAsync($"/chat/{roomName4}", new object());
        response4.StatusCode.Should().Be(HttpStatusCode.OK);

        var response5 = await httpClient.PostAsJsonAsync($"/chat/{roomName5}", new object());
        response5.StatusCode.Should().Be(HttpStatusCode.OK);

        var response6 = await httpClient.PostAsJsonAsync($"/chat/{roomName6}", new object());
        response6.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response6.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(UserInformation.CantAddMoreRooms);
    }

    [Fact]
    public async Task EnterRoom_ShouldReceiveMessage_DetailWelcomeMessage()
    {
        var messageSend = false;
        const string roomName1 = "1";
        const string roomName2 = "2";
        const string userName = "Fabian";

        var (jwtHernan, _) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");
        var (jwtFabian, userIdFabian) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Fabian");

        var httpClientHernan = _fixture.PrivateChatWebApi.CreateClient();
        httpClientHernan.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtHernan}");

        var httpClientFabian = _fixture.PrivateChatWebApi.CreateClient();
        httpClientFabian.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwtFabian}");

        await using var hubHernan = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtHernan);
        await using var hubFabian= _fixture.PrivateChatWebApi.CreateChatHubConnection(jwtFabian);

        await hubFabian.StartAsync();
        await hubHernan.StartAsync();

        var response1 = await httpClientHernan.PostAsJsonAsync($"/chat/{roomName1}", new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClientHernan.PostAsJsonAsync($"/chat/{roomName2}", new object());
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        hubHernan.On<string, string, string, string>("ReceiveMessage", (fromUser, fromUserId, roomId, message) =>
        {
            fromUser.Should().Be(EnterRoomHandler.SystemName);
            fromUserId.Should().Be(Guid.Empty.ToString());
            roomId.Should().Be(roomName1);
            message.Should().Be(string.Format(EnterRoomHandler.SystemWelcomeMessage, userName, userIdFabian));

            messageSend = true;
        });

        var response3 = await httpClientFabian.PostAsJsonAsync($"/chat/{roomName1}", new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(3000);

        messageSend.Should().BeTrue();
    }
}
