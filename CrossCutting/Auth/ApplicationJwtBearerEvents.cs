using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Auth;
public class ApplicationJwtBearerEvents : JwtBearerEvents
{
    public override Task AuthenticationFailed(AuthenticationFailedContext context)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager>();
        
        var userId = context.HttpContext.GetNameIdentifier();
        var userInfo = userManager.GetUser(userId);

        userManager.DisconnectClient(userInfo.ConnectionId);

        return base.AuthenticationFailed(context);
    }
}
