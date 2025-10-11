using System.ComponentModel.DataAnnotations;

namespace WNAB.Data;

public class CategoryAllocation
{
    public int Id { get; set; }
    
    public int CategoryId { get; set; }
    
    public decimal BudgetedAmount { get; set; }
    
    [Range(1, 12)]
    public int Month { get; set; }
    
    public int Year { get; set; }
    
    [MaxLength(40)]
    public string? EditorName { get; set; }
    
    public decimal? PercentageAllocation { get; set; }
    
    public decimal? OldAmount { get; set; }
    
    public string? EditedMemo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Category Category { get; set; } = null!;
    public ICollection<TransactionSplit> TransactionSplits { get; set; } = new List<TransactionSplit>();

    // Parameterless ctor for EF Core
    public CategoryAllocation() { }
}