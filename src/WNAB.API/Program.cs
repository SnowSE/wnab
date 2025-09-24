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
app.MapGet("/users/create", async (string name, string email, WnabContext db) =>
{
    var user = new User { Email = email, FirstName = name, LastName = name };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapGet("/categories/create", async (string name, int userId, WnabContext db) =>
{
    var category = new Category { Name = name, UserId = userId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Ok(category);
});

app.MapGet("/users/accounts/create", async (string name, int userId, WnabContext db) =>
{
    var account = new Account { UserId = userId, AccountName = name, AccountType = "bank", User = db.Users.First((p) => p.Id == userId) };
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Ok(account.Id);
});

app.MapGet("/categories/allocation/create", async (int categoryId, decimal budgetedAmount, int month, int year, WnabContext db) =>
{
    var allocation = new CategoryAllocation
    {
        CategoryId = categoryId,
        BudgetedAmount = budgetedAmount,
        Month = month,
        Year = year
    };
    
    db.Allocations.Add(allocation);
    await db.SaveChangesAsync();
    return Results.Ok(allocation.Id);
});

// New RESTful create endpoints (POST)
app.MapPost("/users", async (CreateUserRequest req, WnabContext db) =>
{
    var user = new User { Email = req.Email, FirstName = req.Name, LastName = req.Name };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.FirstName, user.LastName, user.Email });
});

app.MapPost("/categories", async (CreateCategoryRequest req, WnabContext db) =>
{
    var category = new Category { Name = req.Name, UserId = req.UserId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
});

app.MapPost("/users/{userId}/accounts", async (int userId, CreateAccountRequest req, WnabContext db) =>
{
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    var account = new Account { UserId = userId, AccountName = req.Name, AccountType = "bank", User = user };
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{userId}/accounts/{account.Id}", new { account.Id });
});

app.MapPost("/categories/allocation", async (CreateCategoryAllocationRequest req, WnabContext db) =>
{
    var allocation = new CategoryAllocation
    {
        CategoryId = req.CategoryId,
        BudgetedAmount = req.BudgetedAmount,
        Month = req.Month,
        Year = req.Year
    };

    db.Allocations.Add(allocation);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{req.CategoryId}/allocation/{allocation.Id}", new { allocation.Id });
});

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();

// Request DTOs for POST endpoints
public sealed record CreateUserRequest(string Name, string Email);
public sealed record CreateCategoryRequest(string Name, int UserId);
public sealed record CreateAccountRequest(string Name);
public sealed record CreateCategoryAllocationRequest(int CategoryId, decimal BudgetedAmount, int Month, int Year);
