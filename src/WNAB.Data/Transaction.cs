using System.ComponentModel.DataAnnotations;

namespace WNAB.Data;

public class Transaction
{
    public int Id { get; set; }
    
    public int AccountId { get; set; }
    public string Payee { get; set; } = string.Empty;
        
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = null!;
    
    public decimal Amount { get; set; }
    
    public DateTime TransactionDate { get; set; }
    
    [MaxLength(100)]
    public string? PlaidTransactionId { get; set; }
    
    public bool IsReconciled { get; set; } = false;
        
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public ICollection<TransactionSplit> TransactionSplits { get; set; } = new List<TransactionSplit>();

    // Parameterless ctor for EF Core
    public Transaction() { }
}