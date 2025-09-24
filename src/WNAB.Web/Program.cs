using WNAB.Web.Components;
using Microsoft.Extensions.Hosting; // LLM-Dev: For AddServiceDefaults extension

var builder = WebApplication.CreateBuilder(args);

// LLM-Dev: Enable Aspire service defaults (service discovery + resilience for HttpClient).
builder.AddServiceDefaults();

// LLM-Dev: Register a named HttpClient for the API.
// - If ApiBaseUrl is provided (via AppHost env var), use it.
// - Otherwise, default to logical service name "http://wnab-api" (resolved by service discovery when running under AppHost).
var configuredApi = builder.Configuration["ApiBaseUrl"];
var apiBase = string.IsNullOrWhiteSpace(configuredApi) ? "http://wnab-api" : configuredApi!;
if (!apiBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !apiBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
{
    apiBase = $"http://{apiBase.TrimStart('/')}";
}
builder.Services.AddHttpClient("wnab-api", client => client.BaseAddress = new Uri(apiBase));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
