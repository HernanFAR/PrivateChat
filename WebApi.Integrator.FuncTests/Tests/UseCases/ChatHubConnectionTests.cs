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
}