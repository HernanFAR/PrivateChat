﻿using CrossCutting.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using System.Text;
using CrossCutting;
using Domain;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;
using VSlices.Core.Presentation.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.CreateUser;

public record CreateUserContract(string Name);

public class CreateUserEndpoint : IEndpointDefinition
{
    public const string Url = "/api/user";

    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost(Url, CreateUser)
            .WithName(nameof(CreateUser))
            .Produces<CreateUserCommandResponse>()
            .Produces<string[]>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<CreateUserCommand, CreateUserCommandResponse>, CreateUserHandler>();
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserValidator>();
    }

    public static async Task<IResult> CreateUser(
        [FromServices] ISender sender,
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromBody] CreateUserContract contract)
    {
        var command = new CreateUserCommand(contract.Name);

        var response = await sender.SendAsync(command);

        return response.MatchEndpointResult(TypedResults.Ok);
    }
}

public record CreateUserCommandResponse(string Token);

public record CreateUserCommand(string Name) : ICommand<CreateUserCommandResponse>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public const string NameNotEmptyMessage = "Debes incluir un nombre valido";

    public CreateUserValidator()
    {
        RuleFor(e => e.Name)
            .NotEmpty().WithMessage(NameNotEmptyMessage);
    }
}

public class CreateUserHandler : IHandler<CreateUserCommand, CreateUserCommandResponse>
{
    private readonly IOptionsMonitor<JwtConfiguration> _jwtConfigMonitor;
    private readonly UserManager _userManager;

    public CreateUserHandler(IOptionsMonitor<JwtConfiguration> jwtConfigMonitor, 
        UserManager userManager)
    {
        _jwtConfigMonitor = jwtConfigMonitor;
        _userManager = userManager;
    }

    public async ValueTask<Response<CreateUserCommandResponse>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfigMonitor.CurrentValue.IssuerSigningKey));
        var claims = new Claim[]
        {
            new(ClaimTypes.Name, request.Name),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString().Replace("-", "")),
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var jwt = new JwtSecurityToken(
            issuer: _jwtConfigMonitor.CurrentValue.Issuer,
            audience: _jwtConfigMonitor.CurrentValue.Audience,
            claims: claims,
            expires: DateTime.Now.Add(_jwtConfigMonitor.CurrentValue.Duration),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new CreateUserCommandResponse(tokenHandler.WriteToken(jwt));
    }
}