using Microsoft.AspNetCore.SignalR.Client;
using PrivateChat.CrossCutting.Abstractions;

namespace PrivateChat.CrossCutting;

public class ChatHubConnection : IAsyncDisposable
{
    private readonly Lazy<HubConnection> _connection;
    private IDisposable? _receiveSuscription;

    public ChatHubConnection(IApplicationStorage applicationStorage, Uri serviceUrl)
    {
        _connection = new Lazy<HubConnection>(() =>
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri(serviceUrl, "/websocket/chat"), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(applicationStorage.GetItem<string>(LoginStateProvider.JwtKey));
                })
                .Build();
        });
    }

    public async Task StartAsync(Action<string, string, string, string> onReceiveMessage)
    {
        _receiveSuscription = _connection.Value.On("ReceiveMessage", onReceiveMessage);

        await _connection.Value.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _receiveSuscription?.Dispose();
        await _connection.Value.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}
