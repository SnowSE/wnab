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
}