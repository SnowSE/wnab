using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class Category
{
    public int CategoryId { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(7)] // For hex color codes like #FF5733
    public string? Color { get; set; }
    
    public decimal BudgetAmount { get; set; }
    
    public bool IsIncome { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
