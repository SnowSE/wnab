using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using WNAB.MVM;
using WNAB.Web;
using WNAB.Web.Components;
using WNAB.Web.Services;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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

builder.Services.AddHttpClient("wnab-api", client => client.BaseAddress = new Uri("https+http://wnab-api"))
    .AddHttpMessageHandler<WNAB.Web.AuthenticationDelegatingHandler>();

builder.Services.AddTransient<CategoryManagementService>(sp =>
    new CategoryManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<AccountManagementService>(sp =>
    new AccountManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<CategoryAllocationManagementService>(sp =>
    new CategoryAllocationManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddTransient<TransactionManagementService>(sp =>
    new TransactionManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));

// Register WNAB.MVM services for shared business logic
builder.Services.AddScoped<WebAuthenticationService>();
builder.Services.AddScoped<WNAB.MVM.IAuthenticationService, WebAuthenticationService>();
builder.Services.AddScoped<WNAB.MVM.IMVMPopupService, BlazorPopupService>();
builder.Services.AddScoped<WNAB.MVM.IAlertService, BlazorAlertService>();

// Register Budget logic services
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IBudgetSnapshotService, BudgetSnapshotService>(sp => new BudgetSnapshotService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
builder.Services.AddScoped<IUserService, UserService>(sp => new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("wnab-api")));
// Use the existing Transient registrations from above (lines 94-97)
builder.Services.AddScoped<ICategoryAllocationManagementService>(sp => sp.GetRequiredService<CategoryAllocationManagementService>());
builder.Services.AddScoped<ITransactionManagementService>(sp => sp.GetRequiredService<TransactionManagementService>());

// Register all Models (business logic layer)
builder.Services.AddScoped<AccountsModel>();
builder.Services.AddScoped<AddCategoryModel>();
builder.Services.AddScoped<EditCategoryModel>();
builder.Services.AddScoped<CategoriesModel>();
builder.Services.AddScoped<TransactionsModel>();
builder.Services.AddScoped<PlanBudgetModel>();

// Register Modal/Popup Models
builder.Services.AddScoped<AddAccountModel>();
builder.Services.AddScoped<AddCategoryModel>();
builder.Services.AddScoped<AddTransactionModel>();
builder.Services.AddScoped<EditTransactionModel>();
builder.Services.AddScoped<EditTransactionSplitModel>();
builder.Services.AddScoped<AddSplitToTransactionModel>();

// Register all ViewModels (UI coordination layer)
builder.Services.AddScoped<AccountsViewModel>();
builder.Services.AddScoped<AddCategoryViewModel>();
builder.Services.AddScoped<EditCategoryViewModel>();
builder.Services.AddScoped<CategoriesViewModel>();
builder.Services.AddScoped<TransactionsViewModel>();
builder.Services.AddScoped<PlanBudgetViewModel>();

// Register Modal/Popup ViewModels
builder.Services.AddScoped<AddAccountViewModel>();
builder.Services.AddScoped<AddCategoryViewModel>();
builder.Services.AddScoped<AddTransactionViewModel>();
builder.Services.AddScoped<EditTransactionViewModel>();
builder.Services.AddScoped<EditTransactionSplitViewModel>();
builder.Services.AddScoped<AddSplitToTransactionViewModel>();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



var app = builder.Build();

app.UseForwardedHeaders();

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
