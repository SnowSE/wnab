using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using WNAB.API;
using WNAB.Logic.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.MapGet("/", () => "Hello World!");

// Query endpoints
app.MapGet("/categories", async (WnabContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .ToListAsync();
    return Results.Ok(categories);
});

// LLM-Dev:v4 Add endpoint to get categories for a specific user (following same pattern as accounts)
app.MapGet("/users/categories", (int userId, WnabContext db) =>
{
    var categories = db.Categories.Where(c => c.UserId == userId && c.IsActive);
    return Results.Ok(categories);
});

app.MapGet("/users", async (WnabContext db) => {
	var users = await db.Users.Include(u => u.Accounts).ToListAsync();
    return Results.Ok(users.Select(u => new {
		u.Id, u.FirstName, u.LastName, Accounts = u.Accounts.Select(a => new {a.AccountName,a.AccountType,a.CachedBalance})
	}));
});

app.MapGet("/users/accounts", (int userId, WnabContext db) =>
{
    var Accounts = db.Accounts.Where((p) => p.UserId == userId);
    return Results.Ok(Accounts);
});

app.MapGet("/categories/allocation", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
        .Where(a => a.CategoryId == categoryId)
        .ToListAsync(); 
        return Results.Ok(allocations);
});

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

// New RESTful create endpoints (POST)
app.MapPost("/users", async (UserRecord rec, WnabContext db) =>
{
    var user = new User { Email = rec.Email, FirstName = rec.FirstName, LastName = rec.LastName };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email });
});

app.MapPost("/categories", async (CategoryRecord rec, WnabContext db) =>
{
    var category = new Category { Name = rec.Name, UserId = rec.UserId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
});

app.MapPost("/users/{userId}/accounts", async (int userId, AccountRecord rec, WnabContext db) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    var account = new Account { UserId = userId, AccountName = rec.Name, AccountType = "bank", User = user };
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{userId}/accounts/{account.Id}", new { account.Id });
});

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
});

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
            CategoryId = splitRecord.CategoryId,
            Amount = splitRecord.Amount,
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
});

app.MapGet("/transactions", async (int? accountId, WnabContext db) =>
{
    // LLM-Dev:v6 No Include() to avoid circular references
    var query = db.Transactions.AsNoTracking();

    if (accountId.HasValue)
        query = query.Where(t => t.AccountId == accountId.Value);

    var transactions = await query.ToListAsync();
    return Results.Ok(transactions);
});

app.MapGet("/accounts/{accountId}/transactions", async (int accountId, WnabContext db) =>
{
    // LLM-Dev:v6 No Include() to avoid circular references
    var transactions = await db.Transactions
        .Where(t => t.AccountId == accountId)
        .AsNoTracking()
        .ToListAsync();
    
    return Results.Ok(transactions);
});

// LLM-Dev:v8 Add endpoint to get all transactions for a specific user across all their accounts
app.MapGet("/users/{userId}/transactions", async (int userId, WnabContext db) =>
{
    // LLM-Dev:v8 Use DTO to include Account/Category names without circular reference
    var transactions = await db.Transactions
        .Where(t => t.Account.UserId == userId)
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
                ts.CategoryId,
                ts.Category.Name,
                ts.Amount,
                ts.Notes
            )).ToList()
        ))
        .AsNoTracking()
        .ToListAsync();
    
    return Results.Ok(transactions);
});

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

