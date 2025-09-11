using WNAB.Core.Models;

namespace WNAB.Core.Services;

public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(int userId, CreateTransactionRequest request);
    Task<Transaction?> GetTransactionAsync(int userId, int transactionId);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(int userId, TransactionFilter? filter = null);
    Task<Transaction?> UpdateTransactionAsync(int userId, int transactionId, UpdateTransactionRequest request);
    Task<bool> DeleteTransactionAsync(int userId, int transactionId);
}

public class CreateTransactionRequest
{
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTransactionRequest
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? Date { get; set; }
    public TransactionType? Type { get; set; }
    public int? AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
    public bool? IsCleared { get; set; }
}

public class TransactionFilter
{
    public int? AccountId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TransactionType? Type { get; set; }
    public bool? IsCleared { get; set; }
    public string? SearchTerm { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}