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

///
// GET ENDPOINTS ----------------------------------------------------------------------------------------------------------------------------------
///

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
app.MapGet("/categories", async (HttpContext context, WNAB.API.Services.UserProvisioningService provisioningService, CategoryDBService categoryService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    var categories = await categoryService.GetCategoriesForUserAsync(user.Id);
    return Results.Ok(categories);
}).RequireAuthorization();

// remove this :| unless we build an admin tool
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

//
app.MapGet("/accounts", async (HttpContext context, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var accounts = await accountsService.GetAccountsForUserAsync(user.Id);
    return Results.Ok(accounts);
}).RequireAuthorization();

//
app.MapGet("/categories/allocation", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
    .Where(a => a.CategoryId == categoryId)
    .ToListAsync();
    return Results.Ok(allocations);
}).RequireAuthorization();

// get transactions for authenticated user, optional accountID to get by account.
app.MapGet("/transactions", async (HttpContext context, int? accountId, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var query = db.Transactions
        .Where(t => t.Account.UserId == user.Id);

    if (accountId.HasValue)
    {
        // Verify the account belongs to the user
        var accountBelongsToUser = await db.Accounts
            .AnyAsync(a => a.Id == accountId.Value && a.UserId == user.Id);
        if (!accountBelongsToUser) return Results.NotFound("Account not found or does not belong to user");

        query = query.Where(t => t.AccountId == accountId.Value);
    }

    var transactions = await query
        .OrderByDescending(t => t.TransactionDate)
        .Select(t => new TransactionResponse(
            t.Id,
            t.AccountId,
            t.Account.AccountName,
            t.Payee,
            t.Description,
            t.Amount,
            t.TransactionDate,
            t.IsReconciled,
            t.CreatedAt,
            t.UpdatedAt
        ))
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(new GetTransactionsResponse(transactions));
}).RequireAuthorization();

// get transactionsplits: optional allocationId - if provided, filter by allocation, else return all for current user
app.MapGet("/transactionsplits", async (HttpContext context, int? allocationId, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Base query: all splits for transactions that belong to the current user's accounts
    var query = db.TransactionSplits
        .Where(ts => ts.Transaction.Account.UserId == user.Id);

    if (allocationId.HasValue)
    {
        // Verify the allocation belongs to a category owned by the user
        var allocationBelongsToUser = await db.Allocations
            .AnyAsync(a => a.Id == allocationId.Value && a.Category.UserId == user.Id);
        if (!allocationBelongsToUser) return Results.NotFound("Allocation not found or does not belong to user");

        query = query.Where(ts => ts.CategoryAllocationId == allocationId.Value);
    }

    var transactionSplits = await query
        .OrderByDescending(ts => ts.Transaction.TransactionDate)
        .Select(ts => new TransactionSplitResponse(
            ts.Id,
            ts.CategoryAllocationId,
            ts.TransactionId,
            ts.CategoryAllocation.Category.Name,
            ts.Amount,
            ts.IsIncome,
            ts.Description
        ))
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(new GetTransactionSplitsResponse(transactionSplits));
}).RequireAuthorization();

///
// POST ENDPOINTS --------------------------------------------------------------------------------------------
///


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

app.MapPut("/accounts/{id}", async (HttpContext context, int id, EditAccountRequest req, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Validate that the ID in the route matches the ID in the request body
    if (id != req.Id) return Results.BadRequest("Account ID mismatch between route and request body.");

    try
    {
        var updatedAccount = await accountsService.UpdateAccountAsync(req.Id, user.Id, req.NewName, req.NewAccountType);
        
        if (updatedAccount is null)
            return Results.NotFound("Account not found or does not belong to the current user.");

        return Results.Ok(new { updatedAccount.Id, updatedAccount.AccountName, updatedAccount.AccountType, updatedAccount.UpdatedAt });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
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
app.MapPost("/transactions", async (HttpContext context, TransactionRecord rec, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Validate account exists and belongs to user
    var account = await db.Accounts
           .FirstOrDefaultAsync(a => a.Id == rec.AccountId && a.UserId == user.Id);
    if (account is null) return Results.NotFound("Account not found or does not belong to user");

    // Validate all category allocations belong to user's categories
    var allocationIds = rec.Splits.Select(s => s.CategoryAllocationId).Distinct().ToList();
    var validAllocations = await db.Allocations
    .Where(a => allocationIds.Contains(a.Id) && a.Category.UserId == user.Id)
        .Select(a => a.Id)
      .ToListAsync();

    if (validAllocations.Count != allocationIds.Count)
    {
        return Results.BadRequest("One or more category allocations do not belong to user");
    }

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
            Description = splitRecord.Notes,
            Transaction = transaction,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
        db.TransactionSplits.Add(split);
    }

    await db.SaveChangesAsync();

    // Return response DTO
    var result = new TransactionResponse(
        transaction.Id,
        transaction.AccountId,
        account.AccountName,
        transaction.Payee,
        transaction.Description,
        transaction.Amount,
        transaction.TransactionDate,
        transaction.IsReconciled,
        transaction.CreatedAt,
        transaction.UpdatedAt
    );

    return Results.Created($"/transactions/{transaction.Id}", result);
}).RequireAuthorization();

///
// DELETE ENPOINTS
///

// delete transaction by id (must belong to current user)
app.MapDelete("/transactions/{id:int}", async (HttpContext context, int id, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var transaction = await db.Transactions
        .Include(t => t.TransactionSplits)
        .Include(t => t.Account)
        .FirstOrDefaultAsync(t => t.Id == id && t.Account.UserId == user.Id);

    if (transaction is null)
    {
        return Results.NotFound("Transaction not found or does not belong to user");
    }

    // Remove splits first to be explicit regardless of cascade settings
    if (transaction.TransactionSplits?.Count >0)
    {
        db.TransactionSplits.RemoveRange(transaction.TransactionSplits);
    }

    db.Transactions.Remove(transaction);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

// delete transaction split by id (must belong to current user via transaction->account)
app.MapDelete("/transactionsplits/{id:int}", async (HttpContext context, int id, WnabContext db, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var split = await db.TransactionSplits
        .Include(ts => ts.Transaction)
        .ThenInclude(t => t.Account)
        .FirstOrDefaultAsync(ts => ts.Id == id && ts.Transaction.Account.UserId == user.Id);

    if (split is null)
    {
        return Results.NotFound("Transaction split not found or does not belong to user");
    }

    db.TransactionSplits.Remove(split);
    await db.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

app.MapPut("/categories/{id}", async (HttpContext context, int id, EditCategoryRequest rec, CategoryDBService categoryService, WNAB.API.Services.UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var category = await categoryService.UpdateCategoryWithValidationAsync(user.Id, id, rec);

        return Results.NoContent(); //TODO: Make this a Results.Created instead because you are a cool person
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

