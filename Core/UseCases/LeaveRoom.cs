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
namespace Core.UseCases.LeaveRoom;

public class LeaveRoomEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapDelete("/chat/{room}", Handle)
            .WithName(nameof(LeaveRoomEndpoint))
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<LeaveRoomCommand, Success>, LeaveRoomHandler>();
    }

    public static async Task<IResult> Handle(
        [FromServices] ISender sender,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromRoute] string room)
    {
        var command = new LeaveRoomCommand(room,
            contextAccessor.HttpContext.GetNameIdentifier(),
            contextAccessor.HttpContext.GetName());

        var response = await sender.SendAsync(command);

        return response.MatchEndpointResult(TypedResults.Ok);
    }
}

public record LeaveRoomCommand(string RoomId, string NameIdentifier, string Name) : ICommand;

// TODO: Agregar IHandler<T> where T : IRequest<Success>
public class LeaveRoomHandler : IHandler<LeaveRoomCommand, Success>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;

    public LeaveRoomHandler(IHubContext<ChatHub, IChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<OneOf<Success, BusinessFailure>> HandleAsync(LeaveRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var userInfo = _userManager.GetUser(request.NameIdentifier);

        var result = userInfo.RemoveRoom(request.RoomId);

        if (result.IsT1)
        {
            return result.AsT1;
        }

        await _chatHub.Groups
            .RemoveFromGroupAsync(userInfo.ConnectionId, request.RoomId, cancellationToken);

        await _chatHub.Clients
            .Group(request.RoomId)
            .ReceiveMessage("System", Guid.Empty.ToString(), $"Se ha desconectado {request.Name}#{request.NameIdentifier}");

        return new Success();
    }
}
