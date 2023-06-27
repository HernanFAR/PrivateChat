using CrossCutting.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using VSlices.Core.Abstracts.BusinessLogic;

namespace CrossCutting;

public class ChatHub : Hub
{
    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var userManager = httpContext
            .RequestServices.GetRequiredService<UserManager>();

        userManager.RegisterUserWithConnectionId(Context.GetNameIdentifier(), Context.ConnectionId);

        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var userManager = httpContext
            .RequestServices.GetRequiredService<UserManager>();

        var nameIdentifier = Context.GetNameIdentifier();
        var name= Context.GetName();

        var rooms = userManager.GetRoomsOfUser(nameIdentifier);

        foreach (var roomId in rooms)
        {
            await Clients.Group(roomId)
                .SendAsync("ReceiveMessage", "System", "NO APLICA", $"Se ha desconectado {name}#{nameIdentifier}");
        }

        userManager.RemoveUser(nameIdentifier);

        await base.OnDisconnectedAsync(exception);
    }
}
