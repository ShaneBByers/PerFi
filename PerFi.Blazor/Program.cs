using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PerFi.Blazor.Auth;
using PerFi.Blazor.Configuration;
using PerFi.Blazor;
using PerFi.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions<ApiOptions>()
	.Bind(builder.Configuration.GetSection(ApiOptions.SectionName));

var apiBaseUrl = builder.Configuration[$"{ApiOptions.SectionName}:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
	apiBaseUrl = "http://localhost:5239";

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<PerFiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<PerFiAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddTransient<CookieRequestHandler>();
builder.Services.AddTransient<AuthMessageHandler>();

builder.Services.AddHttpClient(HttpClientNames.AnonymousApiClient, client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>();

builder.Services.AddHttpClient(HttpClientNames.AuthenticatedApiClient, client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>()
	.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<IAccountTypesApiClient, AccountTypesApiClient>(client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>()
	.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<IInstitutionsApiClient, InstitutionsApiClient>(client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>()
	.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<IAccountsApiClient, AccountsApiClient>(client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>()
	.AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<ISnapshotsApiClient, SnapshotsApiClient>(client =>
{
	client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CookieRequestHandler>()
	.AddHttpMessageHandler<AuthMessageHandler>();

await builder.Build().RunAsync();
