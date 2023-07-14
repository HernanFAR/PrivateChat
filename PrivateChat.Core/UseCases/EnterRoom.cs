﻿using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivateChat.Core.Store.Messages;
using PrivateChat.Core.Store.Rooms;
using PrivateChat.CrossCutting.ChatWebApi;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Presentation;
using VSlices.Core.Abstracts.Responses;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.EnterRoom;

public class EnterRoomDependencyDefinition : IUseCaseDependencyDefinition
{
    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<EnterRoomHandler>();
    }
}

public class EnterRoomCommand : ICommand
{
    public string Id { get; set; } = string.Empty;
}

public class EnterRoomHandler : IHandler<EnterRoomCommand>
{
    private readonly SweetAlertService _swal;
    private readonly ChatWebApiConnection _chatHubWebApi;
    private readonly ChatWebApiConnection.ChatHub _chatHub;
    private readonly IState<RoomsState> _roomsState;
    private readonly ILogger<EnterRoomHandler> _logger;
    private readonly IDispatcher _dispatcher;
    private int _retries;

    public EnterRoomHandler(SweetAlertService swal, ChatWebApiConnection chatHubWebApi,
        ChatWebApiConnection.ChatHub chatHub, IState<RoomsState> roomsState,
        ILogger<EnterRoomHandler> logger, IDispatcher dispatcher)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _chatHub = chatHub;
        _roomsState = roomsState;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async ValueTask<Response<Success>> HandleAsync(EnterRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        //if (_roomsState.Value.LoggedRooms.Any(e => e.Id == request.Id))
        //{
        //    _ = _swal.FireTimedToastMessageAsync("Ya estás en esa habitación", "", SweetAlertIcon.Warning);

        //    return new Error();
        //}

        _ = _swal.FireBlockedMessageAsync("Conectado con el chat", "Espera un momento!");
        await _chatHub.StartIfNotConnectedAsync((userName, userId, roomId, message, dateTime) =>
        {
            _dispatcher.Dispatch(new RoomIncomingMessageAction(roomId));
            _dispatcher.Dispatch(new IncomingMessageAction(userName, userId, roomId, message, dateTime));
        });

        try
        {
            _ = _swal.FireBlockedMessageAsync("Entrando a habitación sesión", "¡Espera un momento!");

            await HandleCoreAsync(request, cancellationToken);
            _dispatcher.Dispatch(new EnterRoomAction(request.Id));

            _ = _swal.FireTimedToastMessageAsync($"¡Bienvenido a {request.Id}!", "", SweetAlertIcon.Info);

            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            return BusinessFailure.Of.UserNotAuthenticated();
        }
        catch (ApiException<ICollection<string>> apiException)
        {
            _ = _swal.FireValidationErrorsMessageAsync(
                "No se ha podido entrar a la sala",
                apiException.Result,
                SweetAlertIcon.Warning);

            return BusinessFailure.Of.DefaultError();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 429)
        {
            _ = _swal.FireAsync("Se han enviado muchas peticiones", "Intente más tarde", SweetAlertIcon.Error);

            return BusinessFailure.Of.DefaultError();
        }
        catch (ApiException)
        {
            _ = _swal.FireAsync("Se ha producido un error interno", "Intente más tarde", SweetAlertIcon.Error);

            return BusinessFailure.Of.DefaultError();
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