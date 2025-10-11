using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WNAB.Data;

public class WnabContextFactory : IDesignTimeDbContextFactory<WnabContext>
{
    public WnabContext CreateDbContext(string[] args)
    {
        // For migrations: load configuration from the API project if present, otherwise use env var.
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WNAB.API"));

        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true);

        var config = configBuilder.Build();
        var cs = config.GetConnectionString("wnabdb") ?? Environment.GetEnvironmentVariable("ConnectionStrings__wnabdb");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("No connection string for migrations. Set ConnectionStrings__wnabdb or run via AppHost.");
        }

        var options = new DbContextOptionsBuilder<WnabContext>()
            .UseNpgsql(cs)
            .Options;

        return new WnabContext(options);
    }
}
