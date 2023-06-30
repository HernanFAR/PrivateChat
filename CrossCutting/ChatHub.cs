﻿using Microsoft.AspNetCore.SignalR;
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
    Task ReceiveMessage(string fromUser, string fromUserId, string roomId, string message);
    
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
                .ReceiveMessage("System", Guid.Empty.ToString(), roomId, $"Se ha desconectado {name}#{nameIdentifier}");
        }

        userManager.RemoveUser(nameIdentifier);

        await base.OnDisconnectedAsync(exception);
    }
}
