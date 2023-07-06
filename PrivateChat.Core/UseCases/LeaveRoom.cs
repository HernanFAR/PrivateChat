using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivateChat.Core.Store.Rooms;
using PrivateChat.CrossCutting.ChatWebApi;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Presentation;
using VSlices.Core.Abstracts.Responses;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.LeaveRoom;

public class LeaveRoomDependencyDefinition : IUseCaseDependencyDefinition
{
    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<LeaveRoomHandler>();
    }
}

public record LeaveRoomCommand(string Id) : ICommand;

public class LeaveRoomHandler : IHandler<LeaveRoomCommand>
{
    private readonly SweetAlertService _swal;
    private readonly ChatWebApiConnection _chatHubWebApi;
    private readonly ILogger<LeaveRoomHandler> _logger;
    private readonly IDispatcher _dispatcher;
    private int _retries;

    public LeaveRoomHandler(SweetAlertService swal, ChatWebApiConnection chatHubWebApi,
        ILogger<LeaveRoomHandler> logger, IDispatcher dispatcher)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async ValueTask<Response<Success>> HandleAsync(LeaveRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            _ = _swal.FireUncloseableToastMessageAsync($"Saliendo de {request.Id}!", "");

            await HandleCoreAsync(request, cancellationToken);
            _dispatcher.Dispatch(new LeaveRoomAction(request.Id));

            _ = _swal.FireTimedToastMessageAsync($"¡Saliste correctamente!", "", SweetAlertIcon.Info);

            return new Success();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 401)
        {
            return BusinessFailure.Of.UserNotAuthenticated();
        }
        catch (ApiException ex)
            when (ex.StatusCode == 404)
        {
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
