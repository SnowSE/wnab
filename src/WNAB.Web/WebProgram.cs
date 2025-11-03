using WNAB.Web;
using WNAB.Web.Components;
using WNAB.Web.Services;
using WNAB.MVM;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// LLM-Dev: Enable Aspire service defaults (service discovery + resilience for HttpClient).
builder.AddServiceDefaults();

// Configure authentication with Keycloak
var keycloakConfig = builder.Configuration.GetSection("Keycloak");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "wnab.auth";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = keycloakConfig["Authority"];
    options.ClientId = keycloakConfig["ClientId"];
    options.ClientSecret = keycloakConfig["ClientSecret"];
    options.ResponseType = keycloakConfig["ResponseType"] ?? OpenIdConnectResponseType.Code;
    options.RequireHttpsMetadata = keycloakConfig.GetValue<bool>("RequireHttpsMetadata");
    options.SaveTokens = true; // Must be true to save tokens for API calls
    options.GetClaimsFromUserInfoEndpoint = keycloakConfig.GetValue<bool>("GetClaimsFromUserInfoEndpoint");

    // Add scopes
    options.Scope.Clear();
    foreach (var scope in keycloakConfig.GetSection("Scopes").Get<string[]>() ?? new[] { "openid", "profile", "email", "wnab-api" })
    {
        options.Scope.Add(scope);
    }

    // Map claims
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.TokenValidationParameters.RoleClaimType = "realm_access.roles";

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            // Additional claim processing if needed
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            context.HandleResponse();
            context.Response.Redirect($"/error?message={Uri.EscapeDataString(context.Exception.Message)}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add HttpContextAccessor for accessing tokens
builder.Services.AddHttpContextAccessor();

// Register the authentication delegating handler
builder.Services.AddTransient<WNAB.Web.AuthenticationDelegatingHandler>();

// LLM-Dev:v2 Centralize base root: define ONE named HttpClient with the service-discovery base URI
// and construct all Logic services with that named client via IHttpClientFactory.
// Add the authentication handler to attach tokens to API requests
builder.Services.AddHttpClient("wnab-api", client => client.BaseAddress = new Uri("https+http://wnab-api"))
    .AddHttpMessageHandler<WNAB.Web.AuthenticationDelegatingHandler>();

builder.Services.AddTransient<UserManagementService>(sp =>
    new UserManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<CategoryManagementService>(sp =>
    new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<AccountManagementService>(sp =>
    new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<CategoryAllocationManagementService>(sp =>
    new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<TransactionManagementService>(sp =>
    new TransactionManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

// Register WNAB.MVM services for shared business logic
builder.Services.AddScoped<WNAB.MVM.IAuthenticationService, WebAuthenticationService>();
builder.Services.AddScoped<WNAB.MVM.IMVMPopupService, BlazorPopupService>();

// Register all Models (business logic layer)
builder.Services.AddScoped<AccountsModel>();
builder.Services.AddScoped<CategoriesModel>();
builder.Services.AddScoped<TransactionsModel>();
builder.Services.AddScoped<PlanBudgetModel>(sp => new PlanBudgetModel(
    sp.GetRequiredService<CategoryManagementService>(),
    sp.GetRequiredService<CategoryAllocationManagementService>(),
    sp.GetRequiredService<TransactionManagementService>(),
    sp.GetRequiredService<WNAB.MVM.IAuthenticationService>()));
builder.Services.AddScoped<UsersModel>();

// Register all ViewModels (UI coordination layer)
builder.Services.AddScoped<AccountsViewModel>();
builder.Services.AddScoped<CategoriesViewModel>();
builder.Services.AddScoped<TransactionsViewModel>();
builder.Services.AddScoped<PlanBudgetViewModel>();
builder.Services.AddScoped<UsersViewModel>();

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

app.MapStaticAssets();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add login/logout endpoints
app.MapGet("/login", () => Results.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties
{
    RedirectUri = "/"
}, new[] { OpenIdConnectDefaults.AuthenticationScheme }));

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/"
    });
    return Results.Redirect("/");
}).RequireAuthorization();

app.Run();
