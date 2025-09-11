using Microsoft.EntityFrameworkCore;
using WNAB.Data;

namespace WNAB.API.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WnabDbContext>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }
    }
}