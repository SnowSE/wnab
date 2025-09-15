using Microsoft.EntityFrameworkCore;
using WNAB.Logic.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure EF Core with Npgsql using the Aspire-provided connection string
var connectionString = builder.Configuration.GetConnectionString("wnabdb");
builder.Services.AddDbContext<WnabContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
