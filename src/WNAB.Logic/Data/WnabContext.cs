using Microsoft.EntityFrameworkCore;

namespace WNAB.Logic.Data;

public class WnabContext : DbContext
{
    public WnabContext(DbContextOptions<WnabContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
}