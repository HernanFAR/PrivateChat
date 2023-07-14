using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SignalRSwaggerGen.Attributes;
using SignalRSwaggerGen.Enums;

namespace CrossCutting;

[SignalRHub(path: ChatHub.Url)]
public interface IChatHub
{
    [SignalRMethod("[Method]", Operation.Get, 
        summary: "Websocket para recibir los mensajes", 
        description: $"Debes escuchar {nameof(ReceiveMessage)}, en \"{ChatHub.Url}\" con los siguientes parámetros, accionado cada vez que recibes un mensaje en una habitación" )]
    Task ReceiveMessage(string fromUser, string fromUserId, string roomId, string message, DateTimeOffset sendDateTime);
    
}

public class ChatHub : Hub<IChatHub>
{
    public const string Url = "/websocket/chat";

    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        ArgumentNullException.ThrowIfNull(httpContext);

        var userManager = httpContext
            .RequestServices.GetRequiredService<UserManager>();

        var userAdded = userManager.UpdateConnectionIdOfUser(Context.GetNameIdentifier(), Context);

        if (userAdded.IsFailure)
        {
            Context.Abort();
        }

        return base.OnConnectedAsync();
    }
}
