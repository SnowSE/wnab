using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class Budget
{
    public int BudgetId { get; set; }
    
    public int UserId { get; set; }
    
    public int CategoryId { get; set; }
    
    public decimal BudgetedAmount { get; set; }
    
    [Range(1, 12)]
    public int Month { get; set; }
    
    public int Year { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}