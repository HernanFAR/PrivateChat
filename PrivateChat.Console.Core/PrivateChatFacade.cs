using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using PrivateChatWebApi;
using System.Net.Http.Headers;

namespace PrivateChat.Console.Core;

public class PrivateChatFacade : IAsyncDisposable
{
    private static readonly Uri ServiceUrl = new("https://privatechat-production.up.railway.app");

    private PrivateChatWebApiConnection _connection;
    private readonly HttpClient _client;
    private readonly ILogger<PrivateChatFacade> _logger;

    private HubConnection? _hubConnection;
    private Action<string, string, string, string> _onReceiveMessageAction;

    public string UserName { get; }
    private string? _token;
    private int _retries;

    private PrivateChatFacade(string userName, PrivateChatWebApiConnection connection, HttpClient client,
        ILogger<PrivateChatFacade> logger, Action<string, string, string, string> onReceiveMessageAction)
    {
        UserName = userName;
        _connection = connection;
        _client = client;
        _logger = logger;
        _onReceiveMessageAction = onReceiveMessageAction;
        _retries = 0;
    }

    public async Task<OneOf<Success, Error<string[]>>> CreateJwtAsync()
    {
        try
        {
            _logger.LogInformation("Creando identidad bajo nombre {Name}", UserName);

            var createUserResponse = await _connection.CreateUserAsync(
                new CreateUserContract
                {
                    Name = UserName
                });

            _token = createUserResponse.Token;

            _logger.LogInformation("Creando nueva conexión con el hub");

            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(ServiceUrl, "/websocket/chat"), options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_token);
                })
                .Build();

            _hubConnection.On("ReceiveMessage", _onReceiveMessageAction);

            await _hubConnection.StartAsync();
        }
        catch (ApiException<ICollection<string>> ex)
            when (ex.StatusCode == 422)
        {
            return new Error<string[]>(ex.Result.ToArray());
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _logger.LogInformation("Se han enviado demasiadas peticiones, esperando 1seg antes de continuar");
            await Task.Delay(1000);

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;
            return await CreateJwtAsync();

        }

        _retries = 0;

        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer {_token}");

        return new Success();
    }

    public async Task<OneOf<Success, Error<string[]>>> EnterRoomAsync(string roomId)
    {
        try
        {
            await _connection.EnterRoomAsync(roomId);

            _retries = 0;
            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            _logger.LogWarning("Se ha invalidado tu token identificador, se creará una conexión nueva bajo el mismo nombre y se te conectará a la sala");

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;

            _ = await CreateJwtAsync();
            return await EnterRoomAsync(roomId);
        }
        catch (ApiException<ICollection<string>> ex)
            when (ex.StatusCode == 422)
        {
            return new Error<string[]>(ex.Result.ToArray());
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _logger.LogInformation("Se han enviado demasiadas peticiones, esperando 1seg antes de continuar");
            await Task.Delay(1000);

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;
            return await EnterRoomAsync(roomId);
        }
    }

    public async Task<OneOf<Success, Error<string[]>>> LeaveRoomAsync(string roomId)
    {
        try
        {
            await _connection.LeaveRoomAsync(roomId);

            _retries = 0;
            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            _logger.LogWarning("Se ha invalidado tu token identificador, se creará una conexión nueva bajo el mismo nombre");

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;

            _ = await CreateJwtAsync();
            return await LeaveRoomAsync(roomId);
        }
        catch (ApiException ex)
            when (ex.StatusCode == 404)
        {
            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _logger.LogInformation("Se han enviado demasiadas peticiones, esperando 1seg antes de continuar");
            await Task.Delay(1000);

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;
            return await LeaveRoomAsync(roomId);
        }
    }

    public async Task<OneOf<Success, Error<string[]>>> SendMessageAsync(string roomId, string message)
    {
        try
        {
            await _connection.SendMessageEndpointAsync(roomId,
                new SendMessageContract
                {
                    Message = message
                });

            _retries = 0;
            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            _logger.LogWarning("Se ha invalidado tu token identificador, se creará una conexión nueva bajo el mismo nombre, se te conectará a la sala y se intentará enviar el mensaje");

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;

            _ = await CreateJwtAsync();
            return await SendMessageAsync(roomId, message);
        }
        catch (ApiException ex)
            when (ex.StatusCode == 404)
        {
            return new Error<string[]>(new[] { "No puedes enviar mensajes a una habitación en la que no te has conectado" });
        }
        catch (ApiException<ICollection<string>> ex)
            when (ex.StatusCode == 422)
        {
            return new Error<string[]>(ex.Result.ToArray());
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _logger.LogInformation("Se han enviado demasiadas peticiones, esperando 1seg antes de continuar");
            await Task.Delay(1000);

            if (_retries >= 3)
                return new Error<string[]>(new[] { "No ha podido sido posible conectarse a la aplicación" });

            _retries++;
            return await SendMessageAsync(roomId, message);
        }
    }

    public static async Task<OneOf<PrivateChatFacade, Error<string[]>>> CreateForNameAsync(
        string userName, 
        Action<string, string, string, string> onReceiveMessageAction, 
        bool showInformationalLogging = false)
    {
        var client = new HttpClient();

        var connection = new PrivateChatWebApiConnection(ServiceUrl.ToString(), client);

        var loggingFactory = LoggerFactory.Create(e =>
        {
            e.AddConsole();
            e.SetMinimumLevel(showInformationalLogging ? LogLevel.Information : LogLevel.Warning);

        });

        var facade = new PrivateChatFacade(
            userName, connection, client, 
            loggingFactory.CreateLogger<PrivateChatFacade>(), 
            onReceiveMessageAction)
        {
            _connection = connection
        };

        var jwtLoaded = await facade.CreateJwtAsync();

        if (jwtLoaded.IsT1)
        {
            return jwtLoaded.AsT1;
        }

        return facade;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
