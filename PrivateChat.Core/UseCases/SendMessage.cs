using ChatHubWebApi;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using PrivateChat.Core.Structs;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.SendMessage;

public record SendMessageCommand(string RoomId, string Message);

public class SendMessageHandler
{
    private readonly SweetAlertService _swal;
    private readonly ChatHubWebApiConnection _chatHubWebApi;
    private readonly ILogger<SendMessageHandler> _logger;
    private int _retries;

    public SendMessageHandler(SweetAlertService swal, ChatHubWebApiConnection chatHubWebApi,
        ILogger<SendMessageHandler> logger)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _logger = logger;
    }

    public async ValueTask<OneOf<Success, Error, AuthenticationFailure>> HandleAsync(SendMessageCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            _ = _swal.FireBlockedMessageAsync("Entrando a habitación sesión", "¡Espera un momento!");

            await HandleCoreAsync(request, cancellationToken);

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
