using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WNAB.Core.Models;

public class Transaction
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; }
    
    public TransactionType Type { get; set; }
    
    public bool IsCleared { get; set; } = false;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public string? PlaidTransactionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public enum TransactionType
{
    Income = 1,
    Expense = 2,
    Transfer = 3
}