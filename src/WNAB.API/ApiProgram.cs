using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using WNAB.API;
using WNAB.API.Extensions;
using WNAB.Data;
using WNAB.SharedDTOs;
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
        options.RequireHttpsMetadata = keycloakConfig.GetValue<bool>("RequireHttpsMetadata");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Accept both "account" (default Keycloak audience) and "wnab-api" (if configured)
            ValidAudiences = new[] { "account", "wnab-api" },
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
// Register accounts DB service
builder.Services.AddScoped<AccountDBService>();
// Register categories DB service
builder.Services.AddScoped<CategoryDBService>();

// Get connection string from Aspire (AppHost). If running API alone, allow env var fallback.
var connectionString = builder.Configuration.GetConnectionString("wnabdb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__wnabdb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No connection string found for 'wnabdb'. Run via AppHost or set env var ConnectionStrings__wnabdb.");
}

// Add Include Error Detail to connection string for debugging  OA 10/24/2025
var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
{
    IncludeErrorDetail = true
};
connectionString = connectionStringBuilder.ToString();

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
app.MapGet("/categories", async (HttpContext context, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var categories = await db.Categories
        .Where(c => c.UserId == user.Id && c.IsActive)
        .AsNoTracking()
        .Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Color,
            c.IsActive
        ))
        .ToListAsync();

    return Results.Ok(categories);
}).RequireAuthorization();

app.MapGet("/users", async (WnabContext db) =>
{
    var users = await db.Users.Include(u => u.Accounts).ToListAsync();
    return Results.Ok(users.Select(u => new
    {
        u.Id,
        u.FirstName,
        u.LastName,
        Accounts = u.Accounts.Select(a => new { a.AccountName, a.AccountType, a.CachedBalance })
    }));
}).RequireAuthorization();

app.MapGet("/accounts", async (HttpContext context, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var accounts = await accountsService.GetAccountsForUserAsync(user.Id);
    return Results.Ok(accounts);
}).RequireAuthorization();

app.MapGet("/categories/allocation", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
    .Where(a => a.CategoryId == categoryId)
    .ToListAsync();
    return Results.Ok(allocations);
}).RequireAuthorization();

// get transactions by account id.
app.MapGet("/transactions/account", async (int accountId, WnabContext db) =>
{
    var transactions = await db.Transactions
    .Where(t => t.AccountId == accountId)
    .Include(t => t.TransactionSplits)
    .ThenInclude(ts => ts.CategoryAllocation)
    .AsNoTracking()
    .ToListAsync();

    return Results.Ok(transactions);
});

// get transactionsplits by category id
app.MapGet("/transactionsplits", async (int AllocationId, WnabContext db) =>
{
    var transactionSplits = await db.TransactionSplits
    .Where(ts => ts.CategoryAllocationId == AllocationId)
    .Include(ts => ts.Transaction)
    .ThenInclude(t => t.Account)
    .Include(ts => ts.CategoryAllocation)
    .AsNoTracking()
    .OrderByDescending(ts => ts.Transaction.TransactionDate)
    .ToListAsync();

    return Results.Ok(transactionSplits);
});

// New RESTful create endpoints (POST) - secured with authorization
app.MapPost("/users", async (UserRecord rec, WnabContext db) =>
{
    var user = new User { Email = rec.Email, FirstName = rec.FirstName, LastName = rec.LastName };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email });
}).RequireAuthorization();

app.MapPost("/categories", async (HttpContext context, CreateCategoryRequest rec, CategoryDBService categoryService, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var category = await categoryService.CreateCategoryWithValidationAsync(user.Id, rec);

        return Results.Created($"/categories/{category.Id}", new CategoryDto(category.Id, category.Name, category.Color, category.IsActive));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

}).RequireAuthorization();

app.MapPost("/accounts", async (HttpContext context, AccountRecord rec, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var account = await accountsService.CreateAccountAsync(user, rec.Name);
    return Results.Created($"/accounts/{account.Id}", new { account.Id });
}).RequireAuthorization();


// create allocation
app.MapPost("/allocations", async (CategoryAllocationRecord rec, WnabContext db) =>
{
    var allocation = new CategoryAllocation
    {
        CategoryId = rec.CategoryId,
        BudgetedAmount = rec.BudgetedAmount,
        Month = rec.Month,
        Year = rec.Year,
        EditorName = rec.EditorName,
        PercentageAllocation = rec.PercentageAllocation,
        OldAmount = rec.OldAmount,
        EditedMemo = rec.EditedMemo,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Allocations.Add(allocation);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{rec.CategoryId}/allocation/{allocation.Id}", new { allocation.Id });
}).RequireAuthorization();

// get allocations for a category
app.MapGet("/allocations", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
    .Where(a => a.CategoryId == categoryId)
    .ToListAsync();
    return Results.Ok(allocations);
}).RequireAuthorization();

// create transaction
app.MapPost("/transactions", async (TransactionRecord rec, WnabContext db) =>
{
    // Validate account exists
    var account = await db.Accounts.FindAsync(rec.AccountId);
    if (account is null) return Results.NotFound($"Account {rec.AccountId} not found");

    // LLM-Dev:v3 ALL DateTimes must be UTC for PostgreSQL
    var utcNow = DateTime.UtcNow;
    var utcTransactionDate = rec.TransactionDate.Kind == DateTimeKind.Utc
    ? rec.TransactionDate
    : DateTime.SpecifyKind(rec.TransactionDate, DateTimeKind.Utc);

    var transaction = new Transaction
    {
        AccountId = rec.AccountId,
        Payee = rec.Payee,
        Description = rec.Description,
        Amount = rec.Amount,
        TransactionDate = utcTransactionDate,
        Account = account,
        CreatedAt = utcNow,
        UpdatedAt = utcNow
    };

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync(); // Save to get transaction ID

    // Create transaction splits - ALL DateTimes must be UTC
    foreach (var splitRecord in rec.Splits)
    {
        var split = new TransactionSplit
        {
            TransactionId = transaction.Id,
            CategoryAllocationId = splitRecord.CategoryAllocationId,
            Amount = splitRecord.Amount,
            IsIncome = splitRecord.IsIncome,
            Notes = splitRecord.Notes,
            Transaction = transaction,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
        db.TransactionSplits.Add(split);
    }

    await db.SaveChangesAsync();

    // LLM-Dev:v5 Reload transaction without navigation properties to avoid circular reference
    var result = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transaction.Id);
    return Results.Created($"/transactions/{transaction.Id}", result);
}).RequireAuthorization();

app.MapGet("/transactions", async (HttpContext context, int? accountId, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // LLM-Dev:v8 Use DTO to include Account/Category names without circular reference
    var query = db.Transactions
    .Where(t => t.Account.UserId == user.Id);

    if (accountId.HasValue)
        query = query.Where(t => t.AccountId == accountId.Value);

    var transactions = await query
    .OrderByDescending(t => t.TransactionDate)
    .Select(t => new TransactionDto(
    t.Id,
    t.AccountId,
    t.Account.AccountName,
    t.Payee,
    t.Description,
    t.Amount,
    t.TransactionDate,
    t.IsReconciled,
    t.CreatedAt,
    t.UpdatedAt,
    t.TransactionSplits.Select(ts => new TransactionSplitDto(
    ts.Id,
    ts.CategoryAllocationId,
    ts.TransactionId,
    ts.CategoryAllocation.Category.Name,
    ts.Amount,
    ts.IsIncome,
    ts.Notes
    )).ToList()
    ))
    .AsNoTracking()
    .ToListAsync();

    return Results.Ok(transactions);
}).RequireAuthorization();

app.MapGet("/accounts/{accountId}/transactions", async (HttpContext context, int accountId, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Verify the account belongs to the user
    var accountBelongsToUser = await accountsService.AccountBelongsToUserAsync(accountId, user.Id);
    if (!accountBelongsToUser) return Results.NotFound();

    // LLM-Dev:v6 No Include() to avoid circular references
    var transactions = await db.Transactions
    .Where(t => t.AccountId == accountId)
    .AsNoTracking()
    .ToListAsync();

    return Results.Ok(transactions);
}).RequireAuthorization();

app.MapPut("/categories/{id}", async (HttpContext context, int id, EditCategoryRequest rec, CategoryDBService categoryService, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var category = await categoryService.UpdateCategoryWithValidationAsync(user.Id, id, rec);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

}).RequireAuthorization();

app.MapDelete("/categories/{id}", async (HttpContext context, int id, CategoryDBService categoryService, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        await categoryService.DeleteCategoryWithValidationAsync(user.Id, id);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

