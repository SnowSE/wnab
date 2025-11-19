using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using WNAB.API;
using WNAB.API.Extensions;
using WNAB.Data;
using WNAB.SharedDTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WNAB.API.Services;

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
builder.Services.AddScoped<UserProvisioningService>();
// Register accounts DB service
builder.Services.AddScoped<AccountDBService>();
// Register categories DB service
builder.Services.AddScoped<CategoryDBService>();
// Register allocations DB service
builder.Services.AddScoped<AllocationDBService>();
// Register transactions DB service
builder.Services.AddScoped<TransactionDBService>();
// Register transaction splits DB service
builder.Services.AddScoped<TransactionSplitDBService>();
// Register budget snapshot DB service
builder.Services.AddScoped<IBudgetSnapshotDbService, BudgetSnapshotDbService>();

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
app.MapGet("/api/me", async (HttpContext context, UserProvisioningService provisioningService, WnabContext db) =>
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
app.MapGet("/categories", async (HttpContext context, UserProvisioningService provisioningService, CategoryDBService categoryService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    var categories = await categoryService.GetCategoriesForUserAsync(user.Id);
    return Results.Ok(categories);
}).RequireAuthorization();

//
app.MapGet("/accounts", async (HttpContext context, WnabContext db, UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var accounts = await accountsService.GetAccountsForUserAsync(user.Id);
    return Results.Ok(accounts);
}).RequireAuthorization();

app.MapGet("/allocations/{categoryId:int}", async (HttpContext context, int categoryId, WnabContext db, UserProvisioningService provisioningService, AllocationDBService allocationService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == user.Id);
    if (category is null) return Results.NotFound("Category not found or does not belong to user");

    var allocations = await allocationService.GetAllocationsForCategoryAsync(categoryId);
    return Results.Ok(allocations);
}).RequireAuthorization();

app.MapGet("/allocations", async (HttpContext context, WnabContext db, UserProvisioningService provisioningService, AllocationDBService allocationService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var allocations = await allocationService.GetAllocationsForUserAsync(user.Id);
    return Results.Ok(allocations);
}).RequireAuthorization();

// get transactions for authenticated user, optional accountID to get by account.
app.MapGet("/transactions", async (HttpContext context, int? accountId, WnabContext db, UserProvisioningService provisioningService, TransactionDBService transactionService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    if (accountId.HasValue)
    {
        // Verify the account belongs to the user
        var accountBelongsToUser = await db.Accounts
            .AnyAsync(a => a.Id == accountId.Value && a.UserId == user.Id);
        if (!accountBelongsToUser) return Results.NotFound("Account not found or does not belong to user");
    }

    var transactions = await transactionService.GetTransactionsForUserAsync(user.Id, accountId);
    return Results.Ok(new GetTransactionsResponse(transactions));
}).RequireAuthorization();

app.MapGet("/transactions/{id:int}", async (HttpContext context, int id, WnabContext db, UserProvisioningService provisioningService, TransactionDBService transactionService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var transaction = await transactionService.GetTransactionByIdAsync(id, user.Id);

    if (transaction is null)
        return Results.NotFound("Transaction not found or does not belong to user");

    return Results.Ok(transaction);
}).RequireAuthorization();

app.MapGet("/transactionsplits", async (HttpContext context, int? allocationId, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    if (allocationId.HasValue)
    {
        // Verify the allocation belongs to a category owned by the user
        var allocationBelongsToUser = await transactionSplitService.AllocationBelongsToUserAsync(allocationId.Value, user.Id);
        if (!allocationBelongsToUser) return Results.NotFound("Allocation not found or does not belong to user");
    }

    var transactionSplits = await transactionSplitService.GetTransactionSplitsForUserAsync(user.Id, allocationId);
    return Results.Ok(new GetTransactionSplitsResponse(transactionSplits));
}).RequireAuthorization();

app.MapGet("/transactionsplitsbymonth", async (HttpContext context, int month, int year, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var transactionSplits = await transactionSplitService.GetTransactionSplitsForUserByMonthAsync(user.Id, month, year);

    return Results.Ok(new GetTransactionSplitsResponse(transactionSplits));
});

app.MapGet("/transactionsplits/{id:int}", async (HttpContext context, int id, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var split = await transactionSplitService.GetTransactionSplitByIdAsync(id, user.Id);

    if (split is null)
        return Results.NotFound("Transaction split not found or does not belong to user");

    return Results.Ok(split);
}).RequireAuthorization();

///
// POST ENDPOINTS --------------------------------------------------------------------------------------------
///

app.MapPost("/categories", async (HttpContext context, CreateCategoryRequest rec, CategoryDBService categoryService, UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var category = await categoryService.CreateCategoryWithValidationAsync(user.Id, rec);

        return Results.Created($"/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, category.Color, category.IsActive));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

}).RequireAuthorization();

app.MapPost("/accounts", async (HttpContext context, AccountRecord rec, WnabContext db, UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();
    try{
        var account = await accountsService.CreateAccountAsync(user, rec.Name, rec.AccountType);

        return Results.Created($"/accounts/{account.Id}", new { account.Id });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// create allocation
app.MapPost("/allocations", async (HttpContext context, CategoryAllocationRecord rec, WnabContext db, UserProvisioningService provisioningService, AllocationDBService allocationService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Validate that the category exists and belongs to the user
    var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == rec.CategoryId && c.UserId == user.Id);
    if (category is null) 
        return Results.NotFound("Category not found or does not belong to user");

    try
    {
        var allocation = await allocationService.CreateAllocationAsync(
            rec.CategoryId,
            rec.BudgetedAmount,
            rec.Month,
            rec.Year,
            rec.EditorName,
            rec.PercentageAllocation,
            rec.OldAmount,
            rec.EditedMemo
        );

        return Results.Created($"/categories/{rec.CategoryId}/allocation/{allocation.Id}", new { allocation.Id });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new { Error = ex.Message });
    }
}).RequireAuthorization();

// create transaction
app.MapPost("/transactions", async (HttpContext context, TransactionRecord rec, WnabContext db, UserProvisioningService provisioningService, TransactionDBService transactionService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var result = await transactionService.CreateTransactionAsync(
            rec.AccountId,
            user.Id,
            rec.Payee,
            rec.Description,
            rec.Amount,
            rec.TransactionDate,
            rec.Splits
        );

        return Results.Created($"/transactions/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// Create transaction split (add split to existing transaction)
app.MapPost("/transactionsplits", async (HttpContext context, TransactionSplitRecord rec, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var response = await transactionSplitService.CreateTransactionSplitAsync(
            rec.TransactionId,
            rec.CategoryAllocationId,
            user.Id,
            rec.Amount,
            rec.Notes
        );

        return Results.Created($"/transactionsplits/{response.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

///
// PUT ENDPOINTS -------------------------------------------------------------------------
///

app.MapPut("/categories/{id}", async (HttpContext context, int id, EditCategoryRequest rec, CategoryDBService categoryService, UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var category = await categoryService.UpdateCategoryWithValidationAsync(user.Id, id, rec);

        return Results.Created($"/categories/{category.Id}", new CategoryResponse(category.Id, category.Name, category.Color, category.IsActive));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

}).RequireAuthorization();

app.MapPut("/accounts/{id}", async (HttpContext context, int id, EditAccountRequest req, WnabContext db, UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var updatedAccount = await accountsService.UpdateAccountAsync(id, user.Id, req.NewName, req.NewAccountType, req.IsActive, req.Id);

        if (updatedAccount is null)
            return Results.NotFound("Account not found or does not belong to the current user.");

        return Results.Ok(new { updatedAccount.Id, updatedAccount.AccountName, updatedAccount.AccountType, updatedAccount.IsActive, updatedAccount.UpdatedAt });
    }
    catch (ArgumentException ex) when (ex.Message.Contains("mismatch"))
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
    {
        return Results.Conflict(new { error = ex.Message });
    }
}).RequireAuthorization();

// Update transaction by id (must belong to current user)
app.MapPut("/transactions/{id}", async (HttpContext context, int id, EditTransactionRequest req, WnabContext db, UserProvisioningService provisioningService, TransactionDBService transactionService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Validate that the ID in the route matches the ID in the request body
    if (id != req.Id) return Results.BadRequest("Transaction ID mismatch between route and request body.");

    try
    {
        var result = await transactionService.UpdateTransactionAsync(
            id,
            user.Id,
            req.AccountId,
            req.Payee,
            req.Description,
            req.Amount,
            req.TransactionDate,
            req.IsReconciled
        );

        return Results.Created($"/transactions/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// Update transaction split by id (must belong to current user via transaction->account)
app.MapPut("/transactionsplits/{id}", async (HttpContext context, int id, EditTransactionSplitRequest req, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Validate that the ID in the route matches the ID in the request body
    if (id != req.Id) return Results.BadRequest("Transaction split ID mismatch between route and request body.");

    try
    {
        var result = await transactionSplitService.UpdateTransactionSplitAsync(
            id,
            user.Id,
            req.CategoryAllocationId,
            req.Amount,
            req.Description
        );

        return Results.Created($"/transactionsplits/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// update allocation
app.MapPut("/allocations/{id}", async (HttpContext context, int id, UpdateCategoryAllocationRequest req, WnabContext db, UserProvisioningService provisioningService, AllocationDBService allocationService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    // Verify allocation belongs to user
    var allocationBelongsToUser = await allocationService.AllocationBelongsToUserAsync(id, user.Id);
    if (!allocationBelongsToUser)
        return Results.Forbid();

    try
    {
        var allocation = await allocationService.UpdateAllocationAsync(
            id,
            req.BudgetedAmount,
            req.IsActive,
            req.EditorName,
            req.EditedMemo
        );

        return Results.Ok(new { allocation.Id, allocation.BudgetedAmount, allocation.IsActive });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

///
// DELETE ENDPOINTS ------------------------------------
/// 
/// 
/// 

// Delete account by id (must belong to current user)
app.MapDelete("/accounts/{id}", async (HttpContext context, int id, WnabContext db, UserProvisioningService provisioningService, AccountDBService accountsService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        var (success, errorMessage) = await accountsService.DeactivateAccountAsync(id, user.Id);

        if (!success)
        {
            return errorMessage switch
            {
                "Invalid account ID." => Results.BadRequest(errorMessage),
                "Account not found." => Results.NotFound(errorMessage),
                "Account does not belong to the current user." => Results.Forbid(),
                _ => Results.BadRequest("An error occurred while deleting the account.")
            };
        }

        return Results.Ok(); // maybe we should change to Results.NoContent?
    }
    catch (InvalidOperationException ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
}).RequireAuthorization();

app.MapDelete("/categories/{id}", async (HttpContext context, int id, CategoryDBService categoryService, UserProvisioningService provisioningService) =>
{
    var user = await context.GetCurrentUserAsync(categoryService.DbContext, provisioningService);
    if (user is null) return Results.Unauthorized();

    try
    {
        await categoryService.DeleteCategoryWithValidationAsync(user.Id, id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
}).RequireAuthorization();

// delete transaction by id (must belong to current user)
app.MapDelete("/transactions/{id:int}", async (HttpContext context, int id, WnabContext db, UserProvisioningService provisioningService, TransactionDBService transactionService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var deleted = await transactionService.DeleteTransactionAsync(id, user.Id);

    if (!deleted)
    {
        return Results.NotFound("Transaction not found or does not belong to user");
    }

    return Results.NoContent();
}).RequireAuthorization();

// delete transaction split by id (must belong to current user via transaction->account)
app.MapDelete("/transactionsplits/{id:int}", async (HttpContext context, int id, WnabContext db, UserProvisioningService provisioningService, TransactionSplitDBService transactionSplitService) =>
{
    var user = await context.GetCurrentUserAsync(db, provisioningService);
    if (user is null) return Results.Unauthorized();

    var deleted = await transactionSplitService.DeleteTransactionSplitAsync(id, user.Id);

    if (!deleted)
    {
        return Results.NotFound("Transaction split not found or does not belong to user");
    }

    return Results.NoContent();
}).RequireAuthorization();

// Map budget endpoints
app.MapBudgetEndpoints();


// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

