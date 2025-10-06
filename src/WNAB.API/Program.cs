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

// "/"
app.MapGet("/", () => "Hello World!");

// =========================================================================
// these are unsafe and need to be secured or removed.
// get all categories
app.MapGet("/all/categories", async (WnabContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .ToListAsync();
    return Results.Ok(categories);
});

// get all users
app.MapGet("/all/users", async (WnabContext db) => {
	var users = await db.Users.Include(u => u.Accounts).ToListAsync();
    return Results.Ok(users.Select(u => new {
		u.Id, u.FirstName, u.LastName, Accounts = u.Accounts.Select(a => new {a.AccountName,a.AccountType,a.CachedBalance})
	}));
});

// ===========================================================================


// get categories by user id.
app.MapGet("/categories", (int userId, WnabContext db) =>
{
    var categories = db.Categories.Where(c => c.UserId == userId && c.IsActive);
    return Results.Ok(categories);
});


// get accounts by user id
app.MapGet("/accounts", (int userId, WnabContext db) =>
{
    var Accounts = db.Accounts.Where((p) => p.UserId == userId);
    return Results.Ok(Accounts);
});

// get allocations by category id...?
app.MapGet("/allocations", async (int categoryId, WnabContext db) =>
{
    var allocations = await db.Allocations
        .Where(a => a.CategoryId == categoryId)
        .ToListAsync(); 
        return Results.Ok(allocations);
});

// get transactions by account id.
app.MapGet("/transactions/account", async (int accountId, WnabContext db) =>
{
    var transactions = await db.Transactions
        .Where(t => t.AccountId == accountId)
        .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.Category)
        .AsNoTracking()
        .ToListAsync();
    
    return Results.Ok(transactions);
});

// get transactions by user id
app.MapGet("/transactions", async (int userId, WnabContext db) =>
{
    var transactions = await db.Transactions
        .Where(t => t.Account.UserId == userId)
        .Include(t => t.TransactionSplits)
        .ThenInclude(ts => ts.Category)
        .Include(t => t.Account)
        .AsNoTracking()
        .OrderByDescending(t => t.TransactionDate)
        .ToListAsync();
    
    return Results.Ok(transactions);
});

// get transactionsplits by category id
app.MapGet("/transactionsplits", async (int CategoryId, WnabContext db) => {
    var transactionSplits = await db.TransactionSplits
        .Where(ts => ts.CategoryId == CategoryId)
        .Include(ts => ts.Transaction)
        .ThenInclude(t => t.Account)
        .Include(ts => ts.Category)
        .AsNoTracking()
        .OrderByDescending(ts => ts.Transaction.TransactionDate)
        .ToListAsync();
    
    return Results.Ok(transactionSplits);
});

// create user
app.MapPost("/users", async (UserRecord rec, WnabContext db) =>
{
    var user = new User { Email = rec.Email, FirstName = rec.FirstName, LastName = rec.LastName };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email });
});

// create category
app.MapPost("/categories", async (CategoryRecord rec, WnabContext db) =>
{
    var category = new Category { Name = rec.Name, UserId = rec.UserId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
});

// create account
app.MapPost("/accounts", async (int userId, AccountRecord rec, WnabContext db) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    var account = new Account { UserId = userId, AccountName = rec.Name, AccountType = "bank", User = user };
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{userId}/accounts/{account.Id}", new { account.Id });
});


// create allocation
app.MapPost("/allocations", async (CategoryAllocationRecord rec, WnabContext db) =>
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
        Amount = rec.Amount,
        TransactionDate = utcTransactionDate,
        Account = account,
        CreatedAt = utcNow,
        UpdatedAt = utcNow
    };

    db.Transactions.Add(transaction);
    await db.SaveChangesAsync(); // Save to get transaction ID


    await db.SaveChangesAsync();
    
    // LLM-Dev:v5 Reload transaction without navigation properties to avoid circular reference
    var result = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transaction.Id);
    return Results.Created($"/transactions/{transaction.Id}", result);
});

// create a split
app.MapPost("/transactionsplits", async (TransactionSplitRecord rec, WnabContext db) => {
    // Validate transaction exists
    var transaction = await db.Transactions.FindAsync(rec.TransactionId);
    if (transaction is null) return Results.NotFound($"Transaction {rec.TransactionId} not found");

    // Validate category exists
    var category = await db.Categories.FindAsync(rec.CategoryId);
    if (category is null) return Results.NotFound($"Category {rec.CategoryId} not found");

    // Create the transaction split
    var transactionSplit = new TransactionSplit
    {
        TransactionId = rec.TransactionId,
        CategoryId = rec.CategoryId,
        Amount = rec.Amount,
        Transaction = transaction,
        Category = category
    };

    db.TransactionSplits.Add(transactionSplit);
    await db.SaveChangesAsync();
    return Results.Created($"/transactionsplits/{transactionSplit.Id}", transactionSplit);
});

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

