using Core.Extensions;
using CrossCutting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
namespace Core.UseCases.SendMessage;

public record SendMessageContract(string Message);

public class SendMessageEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/chat/{room}/message", Handle)
            .WithName(nameof(SendMessageEndpoint))
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<SendMessageCommand, Success>, SendMessageHandler>();
    }

    public static async Task<IResult> Handle(
        [FromServices] ISender sender,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromRoute] string room,
        [FromBody] SendMessageContract contract)
    {
        var command = new SendMessageCommand(room, 
            contract.Message, 
            contextAccessor.HttpContext.GetNameIdentifier(), 
            contextAccessor.HttpContext.GetName());

        var response = await sender.SendAsync(command);

        return response.MatchEndpointResult(TypedResults.Ok);
    }
}

public record SendMessageCommand(string RoomId, string Message, string NameIdentifier, string Name) : ICommand;

public class SendMessageHandler : IHandler<SendMessageCommand, Success>
{
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly UserManager _userManager;

    public SendMessageHandler(IHubContext<ChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<OneOf<Success, BusinessFailure>> HandleAsync(SendMessageCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var userInfo = _userManager.GetUser(request.NameIdentifier);

        var isInRoom = userInfo.Rooms.Contains(request.RoomId);

        if (isInRoom)
        {
            return BusinessFailure.Of.NotFoundResource();
        }

        await _chatHub.Clients.GroupExcept(request.RoomId, userInfo.ConnectionId)
            .SendAsync("ReceiveMessage", request.Name, request.NameIdentifier, request.Message, cancellationToken);

        return new Success();
    }
}