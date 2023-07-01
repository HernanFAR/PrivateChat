using OneOf.Types;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatHubWebApi;
using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.Extensions.Logging;
using PrivateChat.Core.Structs;
using PrivateChat.CrossCutting.Abstractions;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.EnterRoom;

public record EnterRoomCommand(string Id);

public class EnterRoomHandler
{
    private readonly SweetAlertService _swal;
    private readonly ChatHubWebApiConnection _chatHubWebApi;
    private readonly ILogger<EnterRoomHandler> _logger;
    private int _retries;

    public EnterRoomHandler(SweetAlertService swal, ChatHubWebApiConnection chatHubWebApi,
        ILogger<EnterRoomHandler> logger)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _logger = logger;
    }

    public async ValueTask<OneOf<Success, Error, AuthenticationFailure>> HandleAsync(EnterRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            _ = _swal.FireBlockedMessageAsync("Entrando a habitación sesión", "¡Espera un momento!");

            await HandleCoreAsync(request, cancellationToken);
            
            _ = _swal.FireTimedToastMessageAsync($"¡Bienvenido a {request.Id}!", "", SweetAlertIcon.Info);

            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            return new AuthenticationFailure();
        }
        catch (ApiException<ICollection<string>> apiException)
        {
            _ = _swal.FireValidationErrorsMessageAsync(
                "No se ha podido entrar a la sala",
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

    private async Task HandleCoreAsync(EnterRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatHubWebApi.EnterRoomAsync(request.Id, cancellationToken);

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
