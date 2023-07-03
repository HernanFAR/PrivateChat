using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrivateChat.CrossCutting.Abstractions;

// ReSharper disable once CheckNamespace
namespace ChatHubWebApi;

public partial class ChatHubWebApiConnection
{
    public static readonly Uri ServiceUrl = new("https://privatechat-production.up.railway.app");
    private readonly ILogger<ChatHubWebApiConnection> _logger;

    public ChatHubWebApiConnection(HttpClient httpClient,
        ILogger<ChatHubWebApiConnection> logger)
    {
        BaseUrl = ServiceUrl.ToString();

        _logger = logger;
        _httpClient = httpClient;
        _settings = new Lazy<JsonSerializerSettings>(CreateSerializerSettings);
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        _logger.LogInformation("Enviando solicitud {Method} a la ruta: {Url}.", request.Method.Method, url);
    }

    public class ChatHub : IAsyncDisposable
    {
        private readonly IApplicationStorage _applicationStorage;

        private Action<string, string, string, string>? _onReceivingMessageAction;
        private HubConnection _connection;
        private IDisposable? _receiveSuscription;

        public ChatHub(IApplicationStorage applicationStorage)
        {
            _applicationStorage = applicationStorage;
            _connection = BuildHubConnection();
        }

        private HubConnection BuildHubConnection()
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri(ServiceUrl, "/websocket/chat"), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_applicationStorage.GetItem<string>(LoginStateProvider.JwtKey));
                })
                .Build();
        }

        private async Task OnHubConnectionClosedAsync(Exception? exception)
        {
            _connection = BuildHubConnection();

            await StartAsync(_onReceivingMessageAction ?? throw new InvalidOperationException(nameof(_onReceivingMessageAction)));
        }

        public async Task StartAsync(Action<string, string, string, string> onReceiveMessage)
        {
            _onReceivingMessageAction = onReceiveMessage;
            _receiveSuscription = _connection.On("ReceiveMessage", _onReceivingMessageAction);

            _connection.Closed += OnHubConnectionClosedAsync;
            await _connection.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            _receiveSuscription?.Dispose();

            _connection.Closed -= OnHubConnectionClosedAsync;
            await _connection.DisposeAsync();

            GC.SuppressFinalize(this);
        }
    }

}
