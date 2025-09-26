using WNAB.Web.Components;
using Microsoft.Extensions.Hosting; // LLM-Dev: For AddServiceDefaults extension
using WNAB.Logic; // LLM-Dev: Register services that call the API

var builder = WebApplication.CreateBuilder(args);

// LLM-Dev: Enable Aspire service defaults (service discovery + resilience for HttpClient).
builder.AddServiceDefaults();



// LLM-Dev:v2 Centralize base root: define ONE named HttpClient with the service-discovery base URI
// and construct all Logic services with that named client via IHttpClientFactory.
builder.Services.AddHttpClient("wnab-api", client => client.BaseAddress = new Uri("https+http://wnab-api"));

builder.Services.AddTransient<UserManagementService>(sp =>
    new UserManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<CategoryManagementService>(sp =>
    new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<AccountManagementService>(sp =>
    new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<CategoryAllocationManagementService>(sp =>
    new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

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
