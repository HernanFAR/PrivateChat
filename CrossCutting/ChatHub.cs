using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting;

public interface IChatHub
{
    Task ReceiveMessage(string fromUser, string fromUserId, string message);

}

public class ChatHub : Hub<IChatHub>
{
    public const string Url = "/chat";

    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var userManager = httpContext
            .RequestServices.GetRequiredService<UserManager>();

        var userAdded = userManager.RegisterUserWithContext(Context.GetNameIdentifier(), Context);

        if (userAdded.IsT1)
        {
            Context.Abort();
        }

        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var userManager = httpContext
            .RequestServices.GetRequiredService<UserManager>();

        var nameIdentifier = Context.GetNameIdentifier();
        var name = Context.GetName();

        var rooms = userManager.GetRoomsOfUser(nameIdentifier);

        foreach (var roomId in rooms)
        {
            await Clients.Group(roomId)
                .ReceiveMessage("System", Guid.Empty.ToString(), $"Se ha desconectado {name}#{nameIdentifier}");
        }

        userManager.RemoveUser(nameIdentifier);

        await base.OnDisconnectedAsync(exception);
    }
}
