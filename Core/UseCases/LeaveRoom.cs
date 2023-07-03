using CrossCutting;
using FluentValidation;
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
    public const string Url = "/api/chat/{room}";

    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapDelete(Url, LeaveRoom)
            .WithName(nameof(LeaveRoom))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<LeaveRoomCommand, Success>, LeaveRoomHandler>();
        services.AddScoped<IValidator<LeaveRoomCommand>, LeaveRoomValidator>();
    }

    public static async Task<IResult> LeaveRoom(
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

public class LeaveRoomValidator : AbstractValidator<LeaveRoomCommand>
{
    public const string UserNotRegisteredInUserManager = "Debes conectar con el web socket del chat para enviar mensajes";

    private readonly UserManager _userManager;

    public LeaveRoomValidator(UserManager userManager)
    {
        _userManager = userManager;

        RuleFor(e => e.NameIdentifier)
            .Must(BeRegisterInUserManager).WithMessage(UserNotRegisteredInUserManager);
    }

    private bool BeRegisterInUserManager(string userId)
    {
        return _userManager.GetUserOrDefault(userId) is not null;
    }
}

// TODO: Agregar IHandler<T> where T : IRequest<Success>
public class LeaveRoomHandler : IHandler<LeaveRoomCommand, Success>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;

    public const string SystemName = "System";
    public const string SystemWelcomeMessage = "Se ha desconectado: {0}#{1}.";

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
            .ReceiveMessage(
                SystemName,
                Guid.Empty.ToString(),
                request.RoomId,
                string.Format(SystemWelcomeMessage, request.Name, request.NameIdentifier),
                DateTimeOffset.Now);

        return new Success();
    }
}
