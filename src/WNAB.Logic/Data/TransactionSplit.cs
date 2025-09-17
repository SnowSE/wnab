using System.ComponentModel.DataAnnotations;

namespace WNAB.Logic.Data;

public class TransactionSplit
{
    public int SplitId { get; set; }

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
}
