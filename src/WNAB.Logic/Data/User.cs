using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WNAB.Logic.Data;

[Index(nameof(KeycloakSubjectId), IsUnique = true)]
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

    /// <summary>
    /// Keycloak subject ID (sub claim) - unique identifier from the identity provider
    /// </summary>
    [MaxLength(255)]
    public string? KeycloakSubjectId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<CategoryAllocation> Allocations { get; set; } = new List<CategoryAllocation>();

    // LLM-Dev:v1 Add convenience ctor to construct from API/test DTO
    public User(UserRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        Email = record.Email;
        FirstName = record.FirstName;
        LastName = record.LastName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Parameterless ctor for EF Core
    public User() { }
}