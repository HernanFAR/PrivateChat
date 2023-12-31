﻿using CrossCutting;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;
using VSlices.Core.Presentation.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.SendMessage;

public record SendMessageContract(string Message);

public class SendMessageEndpoint : IEndpointDefinition
{
    public const string Url = "/api/chat/{room}/message";

    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost(Url, Handle)
            .WithName(nameof(SendMessage))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<string[]>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status429TooManyRequests)
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<SendMessageCommand, Success>, SendMessageHandler>();
        services.AddTransient<IValidator<SendMessageCommand>, SendMessageValidator>();
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

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public const string NameNotEmptyMessage = "Debes indicar una habitación para el mensaje";
    public const string MessageNotEmptyMessage = "Debes indicar un mensaje";
    public const string UserNotRegisteredInUserManager = "Debes conectar con el web socket del chat para enviar mensajes";

    private readonly UserManager _userManager;

    public SendMessageValidator(UserManager userManager)
    {
        _userManager = userManager;

        RuleFor(e => e.Name)
            .NotEmpty().WithMessage(NameNotEmptyMessage);

        RuleFor(e => e.Message)
            .NotEmpty().WithMessage(MessageNotEmptyMessage);

        RuleFor(e => e.NameIdentifier)
            .Must(HaveConnectionId).WithMessage(UserNotRegisteredInUserManager);
    }

    private bool HaveConnectionId(string userId)
    {
        return _userManager.Users
            .Where(e => e.Id == userId)
            .Any(e => e.ConnectionId is not null);
    }
}

public class SendMessageHandler : IHandler<SendMessageCommand, Success>
{
    private readonly IHubContext<ChatHub, IChatHub> _chatHub;
    private readonly UserManager _userManager;

    public SendMessageHandler(IHubContext<ChatHub, IChatHub> chatHub, UserManager userManager)
    {
        _chatHub = chatHub;
        _userManager = userManager;
    }

    public async ValueTask<Response<Success>> HandleAsync(SendMessageCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var getResponse = _userManager.Get(request.NameIdentifier);
        var userInfo = getResponse.SuccessValue;

        if (userInfo.ConnectionId is null) throw new UnreachableException($"The user {userInfo.Id} does not have a connection id");

        var isInRoom = userInfo.Rooms.Contains(request.RoomId);

        if (userInfo.ConnectionId is null) throw new UnreachableException($"The user {userInfo.Id} does not have a connection id");
        if (!isInRoom) return BusinessFailure.Of.NotFoundResource();

        await _chatHub.Clients
            .GroupExcept(request.RoomId, userInfo.ConnectionId)
            .ReceiveMessage(
                request.Name,
                request.NameIdentifier,
                request.RoomId,
                request.Message,
                DateTimeOffset.Now);

        return new Success();
    }
}