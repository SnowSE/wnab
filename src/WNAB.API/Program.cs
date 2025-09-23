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

app.MapGet("/categories", async (WnabContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .ToListAsync();
    return Results.Ok(categories);
});

app.MapGet("/users/create", async (string name, string email, WnabContext db) =>
{
    var user = new User { Email = email, FirstName = name, LastName = name };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(user);
});

app.MapGet("/users", async (WnabContext db) => {
	var users = await db.Users.Include(u => u.Accounts).ToListAsync();
    return Results.Ok(users.Select(u => new {
		u.FirstName, u.LastName, Accounts = u.Accounts.Select(a => new {a.AccountName,a.AccountType,a.CachedBalance})
	}));
});

app.MapGet("/categories/create", async (string name, int userId, WnabContext db) =>
{
    var category = new Category { Name = name, UserId = userId };
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Ok(category);
});

app.MapGet("/users/accounts", (int userId, WnabContext db) =>
{
    var Accounts = db.Accounts.Where((p) => p.UserId == userId);
    return Results.Ok(Accounts);
});

app.MapGet("/users/accounts/create", async (string name, int userId, WnabContext db) =>
{
    var account = new Account { UserId = userId, AccountName = name, AccountType = "bank", User = db.Users.First((p) => p.Id == userId)};
    db.Accounts.Add(account);
    await db.SaveChangesAsync();
    return Results.Ok(account.Id);
});


app.MapPost("/categories/allocation/create", async (int categoryId, decimal budgetedAmount, int month, int year, WnabContext db) =>
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
    return Results.Ok(allocation);
});

// Apply EF Core migrations at startup so the database schema is up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}

app.Run();
