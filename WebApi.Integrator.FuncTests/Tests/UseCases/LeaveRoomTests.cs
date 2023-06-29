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
using Core.UseCases.LeaveRoom;
using Domain;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class LeaveRoomTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public LeaveRoomTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LeaveRoom_ShouldHaveRegisterRoomAtUserManager()
    {
        const string roomName = "2";

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response1 = await httpClient.PostAsJsonAsync($"/chat/{roomName}", new object());
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var response2 = await httpClient.DeleteAsync($"/chat/{roomName}");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.GetRoomsOfUser(userId).Should().BeEmpty();
    }

    [Fact]
    public async Task LeaveRoom_ShouldShowUnprocessableEntity_DetailUserNotConnectedToHub()
    {
        const string roomName = "2";

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        var response = await httpClient.DeleteAsync($"/chat/{roomName}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var errors = await response.Content.ReadFromJsonAsync<string[]>();

        errors.Should().Contain(LeaveRoomValidator.UserNotRegisteredInUserManager);
    }

    [Fact]
    public async Task LeaveRoom_ShouldReturnNotFound()
    {
        const string roomName = "1";

        var (jwt, userId) = _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {jwt}");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);

        await hub.StartAsync();

        var response = await httpClient.DeleteAsync($"/chat/{roomName}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }

    [Fact]
    public async Task LeaveRoom_ShouldReceiveMessage_DetailWelcomeMessage()
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

        var response3 = await httpClientFabian.PostAsJsonAsync($"/chat/{roomName1}", new object());
        response3.StatusCode.Should().Be(HttpStatusCode.OK);

        hubHernan.On<string, string, string, string>("ReceiveMessage", (fromUser, fromUserId, roomId, message) =>
        {
            fromUser.Should().Be(LeaveRoomHandler.SystemName);
            fromUserId.Should().Be(Guid.Empty.ToString());
            roomId.Should().Be(roomName1);
            message.Should().Be(string.Format(LeaveRoomHandler.SystemWelcomeMessage, userName, userIdFabian));

            messageSend = true;
        });

        var response4 = await httpClientFabian.DeleteAsync($"/chat/{roomName1}");
        response4.StatusCode.Should().Be(HttpStatusCode.OK);

        await Task.Delay(3000);

        messageSend.Should().BeTrue();
    }
}
