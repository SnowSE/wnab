using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class CategoryAllocation
{
    public int Id { get; set; }
    
    public int CategoryId { get; set; }
    
    public decimal BudgetedAmount { get; set; }
    
    [Range(1, 12)]
    public int Month { get; set; }
    
    public int Year { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public Category Category { get; set; } = null!;

    // LLM-Dev:v1 Convenience ctor from record
    public CategoryAllocation(CategoryAllocationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        CategoryId = record.CategoryId;
        BudgetedAmount = record.BudgetedAmount;
        Month = record.Month;
        Year = record.Year;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Parameterless ctor for EF Core
    public CategoryAllocation() { }
}