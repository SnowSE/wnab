using Microsoft.EntityFrameworkCore;

namespace WNAB.Data;

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsVisible { get; set; }
}

public class WnabContext : DbContext
{
    public WnabContext(DbContextOptions<WnabContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
}