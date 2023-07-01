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
namespace PrivateChat.Core.UseCases.LeaveRoom;

public record LeaveRoomCommand(string Id);

public class LeaveRoomHandler
{
    private readonly SweetAlertService _swal;
    private readonly ChatHubWebApiConnection _chatHubWebApi;
    private readonly ILogger<LeaveRoomHandler> _logger;
    private int _retries;

    public LeaveRoomHandler(SweetAlertService swal, ChatHubWebApiConnection chatHubWebApi,
        ILogger<LeaveRoomHandler> logger)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _logger = logger;
    }

    public async ValueTask<OneOf<Success, Error, AuthenticationFailure>> HandleAsync(LeaveRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await HandleCoreAsync(request, cancellationToken);
            
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
            return new Error();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _ = _swal.FireAsync("Se ha producido un error interno", "Intente más tarde", SweetAlertIcon.Error);

            return new Error();
        }
    }

    private async Task HandleCoreAsync(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _chatHubWebApi.LeaveRoomAsync(request.Id, cancellationToken);

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
