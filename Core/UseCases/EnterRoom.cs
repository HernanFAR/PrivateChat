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
namespace Core.UseCases.EnterRoom;

public class EnterRoomEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/chat/{room}", EnterRoom)
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

public record EnterRoomCommand(string RoomId, string NameIdentifier, string Name) : ICommand;

public class EnterRoomValidator : AbstractValidator<EnterRoomCommand>
{
    public const string UserNotRegisteredInUserManager = "Debes conectar con el web socket del chat para enviar mensajes";

    private readonly UserManager _userManager;

    public EnterRoomValidator(UserManager userManager)
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
public class EnterRoomHandler : IHandler<EnterRoomCommand, Success>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;

    public const string SystemName = "System";
    public const string SystemWelcomeMessage = "Se ha conectado: {0}#{1} ¡Bienvenido!";

    public EnterRoomHandler(IHubContext<ChatHub, IChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<OneOf<Success, BusinessFailure>> HandleAsync(EnterRoomCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var userInfo = _userManager.GetUser(request.NameIdentifier);

        var result = userInfo.AddRoom(request.RoomId);

        if (result.IsT1)
        {
            return result.AsT1;
        }

        await _chatHub.Clients
            .Group(request.RoomId)
            .ReceiveMessage(
                SystemName,
                Guid.Empty.ToString(),
                request.RoomId,
                string.Format(SystemWelcomeMessage, request.Name, request.NameIdentifier));

        await _chatHub.Groups.AddToGroupAsync(userInfo.ConnectionId, request.RoomId, cancellationToken);

        return new Success();
    }
}
