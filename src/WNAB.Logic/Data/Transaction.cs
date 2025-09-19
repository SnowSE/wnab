using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class Transaction
{
    public int TransactionId { get; set; }
    
    public int AccountId { get; set; }
        
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
}