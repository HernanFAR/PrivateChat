using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace CrossCutting.Auth;

public class ApplicationJwtBearerEvents : JwtBearerEvents
{
    public override Task TokenValidated(TokenValidatedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager>();

        userManager.RegisterUser(context.Principal.GetNameIdentifier(), context.Principal.GetName(), DateTimeOffset.Now);

        return base.TokenValidated(context);
    }

}
