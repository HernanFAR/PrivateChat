using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Abstracts.Presentation;
using VSlices.Core.Abstracts.Responses;
using VSlices.Core.Abstracts.Sender;
using CrossCutting.Auth;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.CreateUser;

public record CreateUserContract(string Name);

public class CreateUserEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/user", Handle)
            .WithName(nameof(CreateUserEndpoint))
            .Produces<CreateUserCommandResponse>();
    }

    public static void DefineDependencies(IServiceCollection services)
    {
        services.AddScoped<IHandler<CreateUserCommand, CreateUserCommandResponse>, CreateUserHandler>();
    }

    public static async Task<IResult> Handle(
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

public class CreateUserHandler : IHandler<CreateUserCommand, CreateUserCommandResponse>
{
    private readonly IOptionsMonitor<JwtConfiguration> _jwtConfigMonitor;

    public CreateUserHandler(IOptionsMonitor<JwtConfiguration> jwtConfigMonitor)
    {
        _jwtConfigMonitor = jwtConfigMonitor;
    }

    public async ValueTask<OneOf<CreateUserCommandResponse, BusinessFailure>> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = new CancellationToken())
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