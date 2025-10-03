using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WNAB.Logic.Data;

public class TransactionSplit
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int CategoryId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public Category Category { get; set; } = null!;
    
    // LLM-Dev: Non-mapped property for test scenarios to handle category by name
    [NotMapped]
    public string CategoryName { get; set; } = string.Empty;

    // LLM-Dev:v2 Convenience ctor from record - fixed property name
    public TransactionSplit(TransactionSplitRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        CategoryId = record.CategoryId;
		TransactionId = record.TransactionId;
        Amount = record.Amount;
        Notes = String.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Parameterless ctor for EF Core
    public TransactionSplit() { }
}
