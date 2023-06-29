using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CrossCutting.Auth;

public class ApplicationJwtBearerEvents : JwtBearerEvents
{
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager>();

        var handler = new JwtSecurityTokenHandler();
        var jwt = context.HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var jsonToken = handler.ReadJwtToken(jwt);

        var userId = jsonToken.Claims
            .SingleOrDefault(e => e.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (userId is null)
        {
            context.Fail("Invalid Token");

            return Task.CompletedTask;
        }

        var userInfo = userManager.GetUser(userId);

        userManager.DisconnectClient(userInfo.ConnectionId);

        context.Fail("Invalid Token");

        return Task.CompletedTask;
    }
}
