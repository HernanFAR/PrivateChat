using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using PrivateChat.Core.Abstractions;
using PrivateChat.Core.Store.Messages;
using PrivateChat.Core.Store.Rooms;
using PrivateChat.Core.Structs;
using System.Security.Claims;
using PrivateChat.CrossCutting.ChatWebApi;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.SendMessage;

public record SendMessageCommand(string RoomId, string Message);

public class SendMessageHandler
{
    private readonly SweetAlertService _swal;
    private readonly ChatHubWebApiConnection _chatHubWebApi;
    private readonly LoginStateProvider _loginStateProvider;
    private readonly ILogger<SendMessageHandler> _logger;
    private readonly IDispatcher _dispatcher;
    private int _retries;

    public SendMessageHandler(SweetAlertService swal, ChatHubWebApiConnection chatHubWebApi,
        LoginStateProvider loginStateProvider,
        ILogger<SendMessageHandler> logger, IDispatcher dispatcher)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _loginStateProvider = loginStateProvider;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async ValueTask<OneOf<Success, Error, AuthenticationFailure>> HandleAsync(SendMessageCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            _ = _swal.FireBlockedMessageAsync("Entrando a habitación sesión", "¡Espera un momento!");

            await HandleCoreAsync(request, cancellationToken);

            var state = await _loginStateProvider.GetAuthenticationStateAsync();

            if (state.User.Identity?.Name is null)
            {
                throw new InvalidOperationException("Usuario sin sesión creada");
            }

            _dispatcher.Dispatch(new RoomIncomingMessageAction(request.RoomId));
            _dispatcher.Dispatch(new IncomingMessageAction(
                state.User.Identity.Name,
                state.User.GetNameIdentifier(),
                request.RoomId,
                request.Message,
                DateTimeOffset.Now));

            _ = _swal.FireTimedToastMessageAsync($"¡Bienvenido a {request.RoomId}!", "", SweetAlertIcon.Info);

            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            return new AuthenticationFailure();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 404)
        {
            _ = _swal.FireValidationErrorsMessageAsync(
                "No se ha podido",
                "No puedes enviar mensajes a esa sala",
                SweetAlertIcon.Warning);

            return new Error();
        }
        catch (ApiException<ICollection<string>> apiException)
        {
            _ = _swal.FireValidationErrorsMessageAsync(
                "No se ha podido",
                apiException.Result,
                SweetAlertIcon.Warning);

            return new Error();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _ = _swal.FireAsync("Se han enviado muchas peticiones", "Intente más tarde", SweetAlertIcon.Error);

            return new Error();
        }
        catch (ApiException ex)
        {
            _ = _swal.FireAsync("Se ha producido un error interno", "Intente más tarde", SweetAlertIcon.Error);

            return new Error();
        }
    }

    private async Task HandleCoreAsync(SendMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = new SendMessageContract
            {
                Message = request.Message
            };

            await _chatHubWebApi.SendMessageAsync(request.RoomId, contract, cancellationToken);

            _retries = 0;
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _logger.LogInformation("Se han enviado demasiadas peticiones, esperando 1seg antes de continuar");
            await Task.Delay(1000, cancellationToken);

            if (_retries >= 3)
            {
                _retries = 0;

                throw;
            }

            _retries++;
            await HandleCoreAsync(request, cancellationToken);
        }
    }
}
