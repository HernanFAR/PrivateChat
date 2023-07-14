using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class ClientDisconnectTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public ClientDisconnectTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ClientDisconnect_ShouldDisconnectClientAfterTimeout_DetailNotConnected()
    {
        // Arrange
        var (_, userId) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");


        // Act
        await Task.Delay(13000);

        // Assert
        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.Get(userId).IsFailure.Should().BeTrue();

    }

    [Fact]
    public async Task ClientDisconnect_ShouldDisconnectClientAfterTimeout_DetailConnectedUser()
    {
        // Arrange
        var (jwt, userId) = await _fixture.PrivateChatWebApi.GenerateJwtTokenForName("Hernán");

        await using var hub = _fixture.PrivateChatWebApi.CreateChatHubConnection(jwt);
        await hub.StartAsync();

        // Act
        await Task.Delay(13000);

        // Assert
        var userManager = _fixture.PrivateChatWebApi.Services.GetRequiredService<UserManager>();

        userManager.Get(userId).IsFailure.Should().BeTrue();

    }
}
