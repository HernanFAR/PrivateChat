using WebApi.Integrator.FuncTests.Factories;

namespace WebApi.Integrator.FuncTests;

public class TestFixture : IAsyncLifetime
{
    public TestFixture()
    {
        PrivateChatWebApi = new PrivateChatWebApiFactory();
    }

    public PrivateChatWebApiFactory PrivateChatWebApi { get; }

    public Task InitializeAsync()
    {
        _ = PrivateChatWebApi.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await PrivateChatWebApi.DisposeAsync();
    }
}
