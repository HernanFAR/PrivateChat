using System.Diagnostics;
using CrossCutting;
using Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;
using VSlices.Core.Presentation.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.EnterRoom;

public class EnterRoomEndpoint : IEndpointDefinition
{
    public const string Url = "/api/chat/{room}";

    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost(Url, EnterRoom)
            .WithName(nameof(EnterRoom))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<string[]>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<EnterRoomCommand, Success>, EnterRoomHandler>();
        services.AddScoped<IValidator<EnterRoomCommand>, EnterRoomValidator>();
    }

    public static async Task<IResult> EnterRoom(
        [FromServices] ISender sender,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromRoute] string room)
    {
        var command = new EnterRoomCommand(room,
            contextAccessor.HttpContext.GetNameIdentifier(),
            contextAccessor.HttpContext.GetName());

        var response = await sender.SendAsync(command);

        return response.MatchEndpointResult(TypedResults.Ok);
    }
}

public record EnterRoomCommand(string RoomId, string UserId, string Name) : ICommand;

public class EnterRoomValidator : AbstractValidator<EnterRoomCommand>
{
    public const string UserNotRegisteredInUserManager = "Debes conectar con el web socket del chat para enviar mensajes";

    private readonly UserManager _userManager;

    public EnterRoomValidator(UserManager userManager)
    {
        _userManager = userManager;

        RuleFor(e => e.UserId)
            .Must(HaveConnectionId).WithMessage(UserNotRegisteredInUserManager);
    }

    private bool HaveConnectionId(string userId)
    {
        return _userManager.Users
            .Where(e => e.Id == userId)
            .Any(e => e.ConnectionId is not null);
    }
}

public class EnterRoomHandler : IHandler<EnterRoomCommand>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;
    
    public const string SystemWelcomeMessage = "Se ha conectado: {0}#{1} ¡Bienvenido!";

    public EnterRoomHandler(IHubContext<ChatHub, IChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<Response<Success>> HandleAsync(EnterRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var getUserResponse = _userManager.Get(request.UserId);
        var user = getUserResponse.SuccessValue;

        var result = user.AddRoom(request.RoomId);

        if (result.IsFailure) return result.BusinessFailure;
        
        await _chatHub.Clients
            .Group(request.RoomId)
            .ReceiveMessage(
                UserInfo.System.Name,
                UserInfo.System.Id,
                request.RoomId,
                string.Format(SystemWelcomeMessage, request.Name, request.UserId),
                DateTimeOffset.Now);

        if (user.ConnectionId is null) throw new UnreachableException($"The user {request.UserId} does not have a connection id");

        await _chatHub.Groups.AddToGroupAsync(user.ConnectionId, request.RoomId, cancellationToken);

        return new Success();
    }
}
