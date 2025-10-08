using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WNAB.Logic.Data;

public class TransactionSplit
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int CategoryAllocationId { get; set; }

    public decimal Amount { get; set; }

    public bool IsIncome { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Transaction Transaction { get; set; } = null!;
    public CategoryAllocation CategoryAllocation { get; set; } = null!;
    
    // LLM-Dev: Non-mapped property for test scenarios to handle category by name
    [NotMapped]
    public string CategoryName { get; set; } = string.Empty;

    // LLM-Dev:v2 Convenience ctor from record - updated to use CategoryAllocation
    public TransactionSplit(TransactionSplitRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        CategoryAllocationId = record.CategoryAllocationId;
        Amount = record.Amount;
        IsIncome = record.IsIncome;
        Notes = record.Notes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Parameterless ctor for EF Core
    public TransactionSplit() { }
}
