using Core.UseCases.ChatHubConnection;
using Core.UseCases.CreateUser;
using Core.UseCases.EnterRoom;
using Core.UseCases.LeaveRoom;
using Core.UseCases.SendMessage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

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
builder.Services.AddSwaggerGen(setup =>
{
    setup.EnableAnnotations();
    setup.AddSignalRSwaggerGen(ssgOptions => ssgOptions.ScanAssemblies(typeof(CrossCutting.Anchor).Assembly));

    setup.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PrivateChatWebApi",
        Version = "v1.1",
        Description = "Una WebApi open-source para mensajería instantánea, sin guardado de información en servidor",
        Contact = new OpenApiContact
        {
            Name = "Hernán Álvarez",
            Email = "h.f.alvarez.rubio@gmail.com",
            Url = new Uri("https://www.linkedin.com/in/hernan-a-rubio/")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
        },
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Pon solamente tu Token JWT Bearer en el input inferior",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

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