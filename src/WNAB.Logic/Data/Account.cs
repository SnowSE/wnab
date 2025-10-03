using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class Account
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = null!;
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal CachedBalance { get; set; }

    [Required]
    public DateTime CachedBalanceDate{ get; set; }
    
    [MaxLength(100)]
    public string? PlaidAccountId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // LLM-Dev:v3 Convenience ctor from record (UserId comes from record, API also passes it separately)
    public Account(AccountRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        AccountName = record.Name;
		UserId = record.UserId;
        AccountType = "bank";
        CachedBalance = 0m;
        CachedBalanceDate = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Parameterless ctor for EF Core
    public Account() { }
}