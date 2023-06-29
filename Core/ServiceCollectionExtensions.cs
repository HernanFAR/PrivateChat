using Core.UseCases.CreateUser;
using CrossCutting;
using CrossCutting.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VSlices.Core.Abstracts.BusinessLogic;
using VSlices.Core.Sender.Reflection;
using VSlices.CrossCutting.Validation.FluentValidation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCrossCuttingConcerns(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();

        services.Configure<JwtConfiguration>(configuration.GetSection(nameof(JwtConfiguration)));

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<UserManager>();

        services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opts =>
            {
                var provider = services.BuildServiceProvider();
                var configMonitor = provider.GetRequiredService<IOptionsMonitor<JwtConfiguration>>();

                opts.Events = new ApplicationJwtBearerEvents();
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configMonitor.CurrentValue.Issuer,
                    ValidAudience = configMonitor.CurrentValue.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(configMonitor.CurrentValue.IssuerSigningKeyBytes),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(5)
                };
            });

        services.AddSender<ReflectionSender>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

        return services;
    }
}
