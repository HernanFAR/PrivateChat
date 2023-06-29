using Core.UseCases.CreateUser;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using CrossCutting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using CrossCutting.Auth;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WebApi.Integrator.FuncTests.Factories;

public class PrivateChatWebApiFactory : WebApplicationFactory<Program>
{
    private bool _disposed;
    private IHost? _host;

    private readonly Uri _baseUrl = new("http://localhost:5000");

    public UserManager UserManagerInstance { get; } = new UserManager();

    public PrivateChatWebApiFactory()
    {
        ClientOptions.AllowAutoRedirect = false;
        ClientOptions.BaseAddress = _baseUrl;
    }

    public Uri ServerAddress
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress;
        }
    }

    public override IServiceProvider Services
    {
        get
        {
            EnsureServer();
            return _host!.Services!;
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create the host for TestServer now before we
        // modify the builder to use Kestrel instead.
        var testHost = builder.Build();

        // Modify the host builder to use Kestrel instead
        // of TestServer so we can listen on a real address.
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        // Create and start the Kestrel server before the test server,
        // otherwise due to the way the deferred host builder works
        // for minimal hosting, the server will not get "initialized
        // enough" for the address it is listening on to be available.
        // See https://github.com/dotnet/aspnetcore/issues/33846.
        _host = builder.Build();
        _host.Start();

        // Extract the selected dynamic port out of the Kestrel server
        // and assign it onto the client options for convenience so it
        // "just works" as otherwise it'll be the default http://localhost
        // URL, which won't route to the Kestrel-hosted HTTP server.
        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        ClientOptions.BaseAddress = addresses!.Addresses
            .Select(x => new Uri(x))
            .Last();

        // Return the host that uses TestServer, rather than the real one.
        // Otherwise the internals will complain about the host's server
        // not being an instance of the concrete type TestServer.
        // See https://github.com/dotnet/aspnetcore/pull/34702.
        testHost.Start();

        return testHost;
    }

    public Task InitializeAsync()
    {
        EnsureServer();

        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());

        builder.ConfigureTestServices(e =>
        {
            e.RemoveAll<UserManager>();
            e.AddSingleton(UserManagerInstance);
        });

        // Configure the server address for the server to
        // listen on for HTTPS requests on a dynamic port.
        builder.UseUrls(_baseUrl.ToString());
    }

    private void EnsureServer()
    {
        if (_host is null)
        {
            // This forces WebApplicationFactory to bootstrap the server
            using var _ = CreateDefaultClient();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_disposed) return;

        if (disposing)
        {
            _host?.Dispose();
        }

        _disposed = true;
    }

    public (string Token, string UserId) GenerateJwtTokenForName(string name, TimeSpan? customDuration = null)
    {
        var userId = Guid.NewGuid().ToString().Replace("-", "");
        var jwtConfigMonitor = Services.GetRequiredService<IOptions<JwtConfiguration>>();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigMonitor.Value.IssuerSigningKey));
        var claims = new Claim[]
        {
            new(ClaimTypes.Name, name),
            new(ClaimTypes.NameIdentifier, userId),
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var jwt = new JwtSecurityToken(
            issuer: jwtConfigMonitor.Value.Issuer,
            audience: jwtConfigMonitor.Value.Audience,
            claims: claims,
            expires: DateTime.Now.Add(customDuration ?? jwtConfigMonitor.Value.Duration),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (tokenHandler.WriteToken(jwt), userId);
    }

    public HubConnection CreateChatHubConnection(string jwt)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(ServerAddress, "chat"), opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult(jwt)!;
                opts.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
    }
}