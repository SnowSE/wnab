using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<CategoryAllocation> Allocations { get; set; } = new List<CategoryAllocation>();
}