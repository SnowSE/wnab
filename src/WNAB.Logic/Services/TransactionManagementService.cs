using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

/// <summary>
/// Handles transaction-related operations via the API.
/// LLM-Dev: Complete transaction service following established patterns. 
/// Supports both simple single-category transactions and complex multi-split transactions.
/// Validates that split amounts sum to transaction total to maintain data integrity.
/// </summary>
public class TransactionManagementService
{
    private readonly HttpClient _http;

    /// <summary>
    /// Construct with an HttpClient configured with API BaseAddress (e.g. https://localhost:7077/)
    /// </summary>
    public TransactionManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    // LLM-Dev: Static factory for transaction record to unify creation without HttpClient/service instances.
    /// <summary>
    /// Creates a <see cref="TransactionRecord"/> DTO from inputs.
    /// </summary>
    public static TransactionRecord CreateTransactionRecord(int accountId, string payee, string description, 
        decimal amount, DateTime transactionDate, List<TransactionSplitRecord> splits)
    {
        if (accountId <= 0) throw new ArgumentOutOfRangeException(nameof(accountId), "AccountId must be positive.");
        if (string.IsNullOrWhiteSpace(payee)) throw new ArgumentException("Payee required", nameof(payee));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description required", nameof(description));
        if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be zero.");
        if (splits == null || !splits.Any()) throw new ArgumentException("At least one split required", nameof(splits));
        
        // Validate splits sum to transaction amount
        var splitsTotal = splits.Sum(s => s.Amount);
        if (Math.Abs(splitsTotal - amount) > 0.01m) // Allow small rounding differences
            throw new ArgumentException($"Splits total ({splitsTotal:C}) must equal transaction amount ({amount:C})", nameof(splits));

        return new TransactionRecord(accountId, payee, description, amount, transactionDate, splits);
    }

    /// <summary>
    /// Helper method to create a simple single-category transaction record.
    /// LLM-Dev:v2 Updated to use CategoryAllocationId instead of CategoryId
    /// </summary>
    public static TransactionRecord CreateSimpleTransactionRecord(int accountId, string payee, string description,
        decimal amount, DateTime transactionDate, int categoryAllocationId, bool isIncome = false, string? notes = null)
    {
        var splits = new List<TransactionSplitRecord>
        {
            new TransactionSplitRecord(categoryAllocationId, amount, isIncome, notes)
        };
        return CreateTransactionRecord(accountId, payee, description, amount, transactionDate, splits);
    }

    /// <summary>
    /// Sends the provided <see cref="TransactionRecord"/> to the API via POST /transactions and returns the created transaction Id.
    /// </summary>
    public async Task<int> CreateTransactionAsync(TransactionRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("transactions", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating transaction.");
        return created.Id;
    }

    /// <summary>
    /// Gets transactions for a specific account.
    /// </summary>
    public async Task<List<Transaction>> GetTransactionsForAccountAsync(int accountId, CancellationToken ct = default)
    {
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>($"accounts/{accountId}/transactions", ct);
        return transactions ?? new();
    }

    /// <summary>
    /// Gets all transactions with optional filtering.
    /// </summary>
    public async Task<List<Transaction>> GetTransactionsAsync(int? accountId = null, CancellationToken ct = default)
    {
        var url = accountId.HasValue ? $"transactions?accountId={accountId.Value}" : "transactions";
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>(url, ct);
        return transactions ?? new();
    }

    /// <summary>
    /// Gets all transactions for the current authenticated user across all their accounts.
    /// LLM-Dev:v2 Returns DTOs to avoid circular reference issues.
    /// </summary>
    public async Task<List<TransactionDto>> GetTransactionsForUserAsync(CancellationToken ct = default)
    {
        var transactions = await _http.GetFromJsonAsync<List<TransactionDto>>("transactions", ct);
        return transactions ?? new();
    }

    // LLM-Dev v1: Helper to convert ViewModel to DTO for simplified service usage
    /// <summary>
    /// Converts a ViewModel with splits to a TransactionRecord DTO.
    /// </summary>
    public static TransactionRecord FromViewModel(int accountId, string payee, string memo, 
        decimal amount, DateTime transactionDate, IEnumerable<TransactionSplitRecord> splits)
    {
        return CreateTransactionRecord(accountId, payee, memo, amount, transactionDate, splits.ToList());
    }
}
