using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using WNAB.API;
using WNAB.Logic.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JWT Bearer authentication with Keycloak
var keycloakConfig = builder.Configuration.GetSection("Keycloak");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakConfig["Authority"];
        options.Audience = keycloakConfig["Audience"];
        options.RequireHttpsMetadata = keycloakConfig.GetValue<bool>("RequireHttpsMetadata");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = keycloakConfig.GetValue<bool>("ValidateAudience"),
            ValidateIssuer = keycloakConfig.GetValue<bool>("ValidateIssuer"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed: {Message}", context.Exception?.Message);
                context.Response.Headers.Append("Authentication-Failed", "true");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated successfully for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var hasAuth = context.Request.Headers.ContainsKey("Authorization");
                logger.LogInformation("Message received. Has Authorization header: {HasAuth}", hasAuth);
                if (hasAuth)
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    var headerLength = authHeader?.Length ?? 0;
                    logger.LogInformation("Authorization header present: {Header}", authHeader?.Substring(0, Math.Min(50, headerLength)));
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register user provisioning service
builder.Services.AddScoped<WNAB.API.Services.UserProvisioningService>();

// Get connection string from Aspire (AppHost). If running API alone, allow env var fallback.
var connectionString = builder.Configuration.GetConnectionString("wnabdb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__wnabdb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No connection string found for 'wnabdb'. Run via AppHost or set env var ConnectionStrings__wnabdb.");
}

// Register a shared NpgsqlDataSource for efficient pooling and re-use it in EF Core.
builder.Services.AddNpgsqlDataSource(connectionString);

builder.Services.AddDbContextPool<WnabContext>((sp, options) =>
{
    var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
    options.UseNpgsql(dataSource);
});

// Database health check (no extra package): simple SELECT 1 using the shared data source.
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql-db");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapOpenApi();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

// User info and provisioning endpoint
app.MapGet("/api/me", async (HttpContext context, WNAB.API.Services.UserProvisioningService provisioningService, WnabContext db) =>
{
    var subjectId = context.User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(subjectId))
    {
        return Results.Unauthorized();
    }

    var email = context.User.FindFirst("email")?.Value ?? context.User.FindFirst("preferred_username")?.Value ?? "unknown@example.com";
    var firstName = context.User.FindFirst("given_name")?.Value;
    var lastName = context.User.FindFirst("family_name")?.Value;

    var user = await provisioningService.GetOrCreateUserAsync(subjectId, email, firstName, lastName);

    return Results.Ok(new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.KeycloakSubjectId,
        user.IsActive
    });
}).RequireAuthorization();

// Query endpoints - secured with authorization
app.MapGet("/categories", async (WnabContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .ToListAsync();
    return Results.Ok(categories);
}).RequireAuthorization();

app.MapGet("/users", async (WnabContext db) => {
	var users = await db.Users.Include(u => u.Accounts).ToListAsync();
    return Results.Ok(users.Select(u => new {
		u.Id, u.FirstName, u.LastName, Accounts = u.Accounts.Select(a => new {a.AccountName,a.AccountType,a.CachedBalance})
	}));
}).RequireAuthorization();

app.MapGet("/users/accounts", (int userId, WnabContext db) =>
{
    var Accounts = db.Accounts.Where((p) => p.UserId == userId);
    return Results.Ok(Accounts);
}).RequireAuthorization();

app.MapGet("/categories/allocation", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
        .Where(a => a.CategoryId == categoryId)
        .ToListAsync();
        return Results.Ok(allocations);
}).RequireAuthorization();

// Legacy create endpoints (GET) - kept for compatibility
// app.MapGet("/users/create", async (string name, string email, WnabContext db) =>
// {
//     var user = new User { Email = email, FirstName = name, LastName = name };
//     db.Users.Add(user);
//     await db.SaveChangesAsync();
//     return Results.Ok(user);
// });

// app.MapGet("/categories/create", async (string name, int userId, WnabContext db) =>
// {
//     var category = new Category { Name = name, UserId = userId };
//     db.Categories.Add(category);
//     await db.SaveChangesAsync();
//     return Results.Ok(category);
// });


// Depreciated.
// app.MapGet("/users/accounts/create", async (string name, int userId, WnabContext db) =>
// {
//     var account = new Account { UserId = userId, AccountName = name, AccountType = "bank", User = db.Users.First((p) => p.Id == userId) };
//     db.Accounts.Add(account);
//     await db.SaveChangesAsync();
//     return Results.Ok(account.Id);
// });

// app.MapGet("/categories/allocation/create", async (int categoryId, decimal budgetedAmount, int month, int year, WnabContext db) =>
// {
//     var allocation = new CategoryAllocation
//     {
//         CategoryId = categoryId,
//         BudgetedAmount = budgetedAmount,
//         Month = month,
//         Year = year
//     };
    
//     db.Allocations.Add(allocation);
//     await db.SaveChangesAsync();
//     return Results.Ok(allocation.Id);
// });

// New RESTful create endpoints (POST) - secured with authorization
app.MapPost("/users", async (UserRecord rec, WnabContext db) =>
{
    var user = new User { Email = rec.Email, FirstName = rec.Name, LastName = rec.Name };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email });
}).RequireAuthorization();

app.MapPost("/categories", async (CategoryRecord rec, WnabContext db) =>
{
    var category = new Category { Name = rec.Name, UserId = rec.UserId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
}).RequireAuthorization();

app.MapPost("/users/{userId}/accounts", async (int userId, AccountRecord rec, WnabContext db) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    var account = new Account { UserId = userId, AccountName = rec.Name, AccountType = "bank", User = user };
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{userId}/accounts/{account.Id}", new { account.Id });
}).RequireAuthorization();

app.MapPost("/categories/allocation", async (CategoryAllocationRecord rec, WnabContext db) =>
{
    var allocation = new CategoryAllocation
    {
        CategoryId = rec.CategoryId,
        BudgetedAmount = rec.BudgetedAmount,
        Month = rec.Month,
        Year = rec.Year
    };

    db.Allocations.Add(allocation);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{rec.CategoryId}/allocation/{allocation.Id}", new { allocation.Id });
}).RequireAuthorization();

app.MapPost("/transactions", async (TransactionRecord rec, WnabContext db) =>
{
    // Validate account exists
    var account = await db.Accounts.FindAsync(rec.AccountId);
    if (account is null) return Results.NotFound($"Account {rec.AccountId} not found");

    // Create the transaction
    var transaction = new Transaction
    {
        AccountId = rec.AccountId,
        Payee = rec.Payee,
        Description = rec.Description,
        Amount = rec.Amount,
        TransactionDate = rec.TransactionDate,
        Account = account
    };

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync(); // Save to get transaction ID

    // Create transaction splits
    foreach (var splitRecord in rec.Splits)
    {
        var split = new TransactionSplit
        {
            TransactionId = transaction.Id,
            CategoryId = splitRecord.CategoryId,
            Amount = splitRecord.Amount,
            Notes = splitRecord.Notes,
            Transaction = transaction
        };
        db.TransactionSplits.Add(split);
    }

    await db.SaveChangesAsync();
    return Results.Created($"/transactions/{transaction.Id}", transaction);
}).RequireAuthorization();

app.MapGet("/transactions", async (int? accountId, WnabContext db) =>
{
    var query = db.Transactions
        .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.Category)
        .Include(t => t.Account)
        .AsNoTracking();

    if (accountId.HasValue)
        query = query.Where(t => t.AccountId == accountId.Value);

    var transactions = await query.ToListAsync();
    return Results.Ok(transactions);
}).RequireAuthorization();

app.MapGet("/accounts/{accountId}/transactions", async (int accountId, WnabContext db) =>
{
    var transactions = await db.Transactions
        .Where(t => t.AccountId == accountId)
        .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.Category)
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(transactions);
}).RequireAuthorization();

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

