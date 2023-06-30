using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.RateLimiting;
using Core.UseCases.ChatHubConnection;
using Core.UseCases.CreateUser;
using Core.UseCases.EnterRoom;
using Core.UseCases.LeaveRoom;
using Core.UseCases.SendMessage;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var portVar = Environment.GetEnvironmentVariable("PORT");
    if (portVar is { Length: > 0 } && int.TryParse(portVar, out var port))
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });
    }
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCrossCuttingConcerns(builder.Configuration);

builder.Services.AddEndpointDefinition<ChatHubConnectionEndpoint>();
builder.Services.AddEndpointDefinition<CreateUserEndpoint>();
builder.Services.AddEndpointDefinition<EnterRoomEndpoint>();
builder.Services.AddEndpointDefinition<LeaveRoomEndpoint>();
builder.Services.AddEndpointDefinition<SendMessageEndpoint>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter(new RateLimiterOptions
{
    GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Path == "/chat")
        {
            return RateLimitPartition.GetNoLimiter("NoLimits");
        }

        return RateLimitPartition.GetTokenBucketLimiter("TokenBased",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                QueueLimit = 0,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 20
            });
    }),
    RejectionStatusCode = StatusCodes.Status429TooManyRequests
});

app.UseEndpointDefinitions();

app.Run();

public partial class Program
{

}