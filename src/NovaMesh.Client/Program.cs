using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using NovaMesh.Client;
using NovaMesh.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddScoped(sp =>
{
    var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
    var configuredAddress = builder.Configuration["services:apiservice:http:0"] 
                         ?? builder.Configuration["services:apiservice:default:0"]
                         ?? builder.Configuration["services:api:http:0"]
                         ?? builder.Configuration["services:api:default:0"];

    var targetUri = !string.IsNullOrWhiteSpace(configuredAddress)
        ? new Uri(configuredAddress)
        : (baseUri.Port is 5000 or 5000 or 5080)
            ? baseUri
            : new Uri("http://localhost:5000");

    return new HttpClient { BaseAddress = targetUri };
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddScoped<TenantStateService>();

await builder.Build().RunAsync();