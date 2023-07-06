using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrivateChat.Core.Abstractions;

// ReSharper disable once CheckNamespace
namespace PrivateChat.CrossCutting.ChatWebApi;

public partial class ChatWebApiConnection
{
    //public static readonly Uri ServiceUrl = new("https://privatechat-production.up.railway.app");
    public static readonly Uri ServiceUrl = new("https://localhost:7253");
    private readonly ILogger<ChatWebApiConnection> _logger;

    public ChatWebApiConnection(HttpClient httpClient,
        ILogger<ChatWebApiConnection> logger)
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
        private readonly ISessionStorage _sessionStorage;

        private Action<string, string, string, string, DateTimeOffset>? _onReceivingMessageAction;
        private HubConnection? _connection;
        private IDisposable? _receiveSuscription;
        private bool _connected;

        public ChatHub(ISessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        private HubConnection BuildHubConnection()
        {
            return new HubConnectionBuilder()
                .WithUrl(new Uri(ServiceUrl, "/websocket/chat"), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_sessionStorage.GetItem<string>(LoginStateProvider.JwtToken));
                })
                .Build();
        }

        private async Task OnHubConnectionClosedAsync(Exception? exception)
        {
            _connection = BuildHubConnection();

            await StartAsync(_onReceivingMessageAction ?? throw new InvalidOperationException(nameof(_onReceivingMessageAction)));
        }

        public async Task StartIfNotConnectedAsync(Action<string, string, string, string, DateTimeOffset> onReceiveMessage)
        {
            if (!_connected)
            {
                await StartAsync(onReceiveMessage);
            }
        }

        public async Task StartAsync(Action<string, string, string, string, DateTimeOffset> onReceiveMessage)
        {
            _connected = true;
            _onReceivingMessageAction = onReceiveMessage;

            _connection = BuildHubConnection();
            await _connection.StartAsync();

            _receiveSuscription = _connection.On("ReceiveMessage", _onReceivingMessageAction);

            _connection.Closed += OnHubConnectionClosedAsync;
        }

        public async ValueTask DisposeIfConnectedAsync()
        {
            if (_connected)
            {
                await DisposeAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _connected = false;
            _receiveSuscription?.Dispose();

            if (_connection is null) throw new InvalidOperationException(nameof(_connected));

            _connection.Closed -= OnHubConnectionClosedAsync;
            await _connection.DisposeAsync();
            _connection = null;

            GC.SuppressFinalize(this);
        }
    }
}
