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

    public TransactionManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

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

    public static TransactionSplitRecord CreateTransactionSplitRecord(int categoryId, int transactionId, decimal amount, string? notes = null)
    {
        if (categoryId <= 0) throw new ArgumentOutOfRangeException(nameof(categoryId), "CategoryId must be positive.");
        if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be zero.");
		if (transactionId)
        return new TransactionSplitRecord(categoryId, transactionId, amount);
    }

    public static TransactionRecord CreateSimpleTransactionRecord(int accountId, string payee, string description,
        decimal amount, DateTime transactionDate, int categoryId, string? notes = null)
    {
        var splits = new List<TransactionSplitRecord>
        {
            CreateTransactionSplitRecord(categoryId, amount, notes)
        };
        return CreateTransactionRecord(accountId, payee, description, amount, transactionDate, splits);
    }


    public async Task<int> CreateTransactionAsync(TransactionRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("transactions", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating transaction.");
        return created.Id;
    }

    public async Task<List<Transaction>> GetTransactionsForAccountAsync(int accountId, CancellationToken ct = default)
    {
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>($"accounts/{accountId}/transactions", ct);
        return transactions ?? new();
    }

    public async Task<List<Transaction>> GetTransactionsAsync(int? accountId = null, CancellationToken ct = default)
    {
        var url = accountId.HasValue ? $"transactions?accountId={accountId.Value}" : "transactions";
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>(url, ct);
        return transactions ?? new();
    }

    public async Task<List<Transaction>> GetTransactionsForUserAsync(int userId, CancellationToken ct = default)
    {
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>($"users/{userId}/transactions", ct);
        return transactions ?? new();
    }


}
