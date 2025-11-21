using Microsoft.EntityFrameworkCore;

namespace WNAB.Data;

public class WnabContext : DbContext
{
    public WnabContext(DbContextOptions<WnabContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryAllocation> Allocations => Set<CategoryAllocation>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<BudgetSnapshot> BudgetSnapshots => Set<BudgetSnapshot>();
    public DbSet<CategorySnapshotData> CategorySnapshotDatas => Set<CategorySnapshotData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Category entity configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Account entity configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CachedBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction entity configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TransactionSplit entity configuration
        modelBuilder.Entity<TransactionSplit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.Transaction)
                .WithMany(t => t.TransactionSplits)
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // LLM-Dev:v3 Changed relationship from Category to CategoryAllocation
            entity.HasOne(e => e.CategoryAllocation)
                .WithMany(ca => ca.TransactionSplits)
                .HasForeignKey(e => e.CategoryAllocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });        

        // Budget entity configuration
        modelBuilder.Entity<CategoryAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BudgetedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OldAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Ensure unique budget per user/category/month/year combination
            entity.HasIndex(e => new { e.CategoryId, e.Month, e.Year }).IsUnique();
                            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Allocations)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete category if budgets exist
        });

        // BudgetSnapshot entity configuration
        modelBuilder.Entity<BudgetSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                .WithMany()  // or add a navigation property on User if desired
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

             // Configure the relationship to CategorySnapshotData
            entity.HasMany(e => e.Categories)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            
            // Optional but recommended: ensure unique snapshot per user/month/year
            entity.HasIndex(e => new { e.UserId, e.Month, e.Year }).IsUnique();
        });

        // CategorySnapshotData entity configuration
        modelBuilder.Entity<CategorySnapshotData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("CategorySnapshotData");
            
            // Configure decimal precision for financial fields
            entity.Property(e => e.AssignedValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Activity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Available).HasColumnType("decimal(18,2)");
            
            // Relationship to Category (optional but recommended for data integrity)
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Index for performance when querying by category
            entity.HasIndex(e => e.CategoryId);
            
            // NOTE: The relationship to BudgetSnapshot is already configured in BudgetSnapshot's
            // HasMany(e => e.Categories) on line 124, so we don't need to duplicate it here.
            // EF Core will handle the inverse relationship automatically.
        });
    }
}