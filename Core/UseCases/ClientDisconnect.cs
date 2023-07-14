using CrossCutting;
using Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Presentation;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.ClientDisconnect;

public class ClientDisconnectedBackgroundService : BackgroundService, IUseCaseDependencyDefinition
{
    private readonly ILogger<ClientDisconnectedBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ClientDisconnectedBackgroundService(ILogger<ClientDisconnectedBackgroundService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();

            var clientDisconnectConfigMonitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<ClientDisconnectConfiguration>>();
            await Task.Delay(clientDisconnectConfigMonitor.CurrentValue.Interval, stoppingToken);

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            _ = await sender.SendAsync(new DisconnectClientCommand(), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Client disconnect service is stopping");

        await base.StopAsync(stoppingToken);
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddOptions<ClientDisconnectConfiguration>()
            .BindConfiguration(nameof(ClientDisconnectConfiguration));

        services.AddHostedService<ClientDisconnectedBackgroundService>();
        services.AddScoped<IHandler<DisconnectClientCommand, Success>, DisconnectClientHandler>();
    }
}

public record DisconnectClientCommand : ICommand;

public class DisconnectClientHandler : IHandler<DisconnectClientCommand>
{
    private readonly UserManager _userManager;
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly IOptionsMonitor<ClientDisconnectConfiguration> _clientDisconnectConfigMonitor;

    public DisconnectClientHandler(UserManager userManager, IHubContext<ChatHub, IChatHub> chatHub,
        IOptionsMonitor<ClientDisconnectConfiguration> clientDisconnectConfigMonitor)
    {
        _userManager = userManager;
        _chatHub = chatHub;
        _clientDisconnectConfigMonitor = clientDisconnectConfigMonitor;
    }

    public async ValueTask<Response<Success>> HandleAsync(DisconnectClientCommand command,
        CancellationToken cancellationToken)
    {
        var users = _userManager.Users
            .Where(u => u.LastInteraction < DateTime.Now.Add(_clientDisconnectConfigMonitor.CurrentValue.LastInteractionDelta))
            .ToArray();

        foreach (var user in users)
        {
            if (string.IsNullOrEmpty(user.ConnectionId))
            {
                _userManager.Remove(user.Id);

                continue;
            }

            var result = _userManager.GetRoomsOfUser(user.Id);

            if (result.IsFailure) continue;

            foreach (var roomId in result.SuccessValue)
            {
                await _chatHub.Groups.RemoveFromGroupAsync(user.ConnectionId, roomId, cancellationToken);

                await _chatHub.Clients.Group(roomId)
                    .ReceiveMessage(
                        UserInfo.System.Name,
                        UserInfo.System.Id,
                        roomId,
                        $"Se ha desconectado {user.Name}#{user.Id}",
                        DateTimeOffset.Now);
            }

            _userManager.Remove(user.Id);
        }

        return new Success();
    }
}

public class ClientDisconnectConfiguration
{
    public TimeSpan LastInteractionDelta { get; init; }

    public TimeSpan Interval { get; init; }
}