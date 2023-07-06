using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using PrivateChat.Core.Abstractions;
using PrivateChat.CrossCutting.ChatWebApi;

// ReSharper disable once CheckNamespace
namespace PrivateChat.Core.UseCases.CreateUser;

public class CreateUserCommand
{
    public string Name { get; set; } = string.Empty;

    public bool Relogin { get; set; }

}

public class CreateUserHandler
{
    private readonly SweetAlertService _swal;
    private readonly ChatHubWebApiConnection _chatHubWebApi;
    private readonly IApplicationLoginProvider _applicationLoginProvider;
    private readonly ILogger<CreateUserHandler> _logger;
    private int _retries;

    public CreateUserHandler(SweetAlertService swal, ChatHubWebApiConnection chatHubWebApi,
        IApplicationLoginProvider applicationLoginProvider, ILogger<CreateUserHandler> logger)
    {
        _swal = swal;
        _chatHubWebApi = chatHubWebApi;
        _applicationLoginProvider = applicationLoginProvider;
        _logger = logger;
    }

    public async ValueTask<OneOf<Success, Error>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            _ = _swal.FireBlockedMessageAsync(request.Relogin ? "Volviendo a iniciar" : "Iniciando sesión", "¡Espera un momento!");

            var token = await HandleCoreAsync(request, cancellationToken);

            await _applicationLoginProvider.Logout(cancellationToken);
            await _applicationLoginProvider.Login(token, cancellationToken);

            _ = _swal.FireTimedToastMessageAsync("¡Sesión iniciada!", "", SweetAlertIcon.Info);

            return new Success();
        }
        catch (ApiException<ICollection<string>> apiException)
        {
            _ = _swal.FireValidationErrorsMessageAsync(
                "No se ha podido iniciar sesión",
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

    private async Task<string> HandleCoreAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = new CreateUserContract
            {
                Name = request.Name
            };

            var response = await _chatHubWebApi.CreateUserAsync(contract, cancellationToken);

            _retries = 0;
            return response.Token;
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
            return await HandleCoreAsync(request, cancellationToken);
        }
    }
}
