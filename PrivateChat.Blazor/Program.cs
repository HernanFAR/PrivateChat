using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrivateChat.Views;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddViewDependencies()
    .AddCrossCuttingDependencies()
    .AddCoreDependenciesFrom<PrivateChat.Core.Anchor>();

await builder.Build().RunAsync();
