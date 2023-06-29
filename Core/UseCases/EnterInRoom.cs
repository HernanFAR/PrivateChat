using Core.Extensions;
using CrossCutting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Presentation;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.EnterInRoom;

public class EnterInRoomEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/chat/{room}", Handle)
            .WithName(nameof(EnterInRoomEndpoint))
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<EnterInRoomCommand, Success>, EnterInRoomHandler>();
    }

    public static async Task<IResult> Handle(
        [FromServices] ISender sender,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromRoute] string room)
    {
        var command = new EnterInRoomCommand(room,
            contextAccessor.HttpContext.GetNameIdentifier(),
            contextAccessor.HttpContext.GetName());

        var response = await sender.SendAsync(command);

        return response.MatchEndpointResult(TypedResults.Ok);
    }
}

public record EnterInRoomCommand(string RoomId, string NameIdentifier, string Name) : ICommand;

// TODO: Agregar IHandler<T> where T : IRequest<Success>
public class EnterInRoomHandler : IHandler<EnterInRoomCommand, Success>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;

    public EnterInRoomHandler(IHubContext<ChatHub, IChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<OneOf<Success, BusinessFailure>> HandleAsync(EnterInRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var userInfo = _userManager.GetUser(request.NameIdentifier);

        await _chatHub.Groups.AddToGroupAsync(userInfo.ConnectionId, request.RoomId, cancellationToken);
        await _chatHub.Clients.Group(request.RoomId)
            .ReceiveMessage("System", Guid.Empty.ToString(), $"Se ha conectado: {request.Name}#{request.NameIdentifier}");

        userInfo.AddRoom(request.RoomId);

        return new Success();
    }
}
