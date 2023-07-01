using CurrieTechnologies.Razor.SweetAlert2;
using Fluxor;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrivateChat.Core.UseCases.CreateUser;
using PrivateChat.Core.UseCases.EnterRoom;
using PrivateChat.Core.UseCases.LeaveRoom;
using PrivateChat.Core.UseCases.SendMessage;
using PrivateChat.CrossCutting;
using PrivateChat.CrossCutting.Abstractions;
using PrivateChat.Views;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddFluxor(options => options.ScanAssemblies(typeof(PrivateChat.Core.Anchor).Assembly));

builder.Services.AddScoped<ChatHubConnection>();
builder.Services.AddScoped<LoginStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<LoginStateProvider>());
builder.Services.AddScoped<IApplicationLoginProvider>(sp => sp.GetRequiredService<LoginStateProvider>());
builder.Services.AddScoped<IApplicationStorage, BrowserLocalStorage>();
builder.Services.AddSweetAlert2();

builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<EnterRoomHandler>();
builder.Services.AddScoped<LeaveRoomHandler>();
builder.Services.AddScoped<SendMessageHandler>();

await builder.Build().RunAsync();
