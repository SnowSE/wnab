using WNAB.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// LLM-Dev: Register a simple named HttpClient for the API.
// Prefer appsetting "ApiBaseUrl" (e.g., "http://localhost:5xxx" when running via AppHost on localhost).
// If not configured, default to a localhost placeholder so it's obvious what to change.
var configuredApi = builder.Configuration["ApiBaseUrl"];
var apiBase = string.IsNullOrWhiteSpace(configuredApi) ? "http://localhost:5001" : configuredApi!;
// Ensure a scheme is present; if a bare host was provided, assume http.
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
