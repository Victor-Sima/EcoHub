using EcoHub.Web;
using EcoHub.Web.Auth;
using EcoHub.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5167";
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHttpHandler>();
    var client = new HttpClient(handler);
    client.BaseAddress = new Uri(apiBase);
    return client;
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
