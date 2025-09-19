using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class Budget
{
    public int BudgetId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    // Month and Year represent the budget period
    public int Month { get; set; } // 1-12
    public int Year { get; set; }

    public decimal BudgetedAmount { get; set; }
    public decimal? SpentAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
