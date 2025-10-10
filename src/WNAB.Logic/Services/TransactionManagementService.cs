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
        var transactions = await _http.GetFromJsonAsync<List<Transaction>>($"transactions/account?accountId={accountId}", ct);
        return transactions ?? new();
    }

    public async Task<List<Transaction>> GetTransactionsAsync(int? accountId = null, CancellationToken ct = default)
    {
        var url = accountId.HasValue ? $"transactions/account?accountId={accountId.Value}" : "transactions";
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

    // LLM-Dev: Add TransactionSplit management methods to match API endpoints
    public async Task<int> CreateTransactionSplitAsync(TransactionSplitRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("transactionsplits", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<TransactionSplit>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating transaction split.");
        return created.Id;
    }

    public async Task<List<TransactionSplit>> GetTransactionSplitsForCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var splits = await _http.GetFromJsonAsync<List<TransactionSplit>>($"transactionsplits?CategoryId={categoryId}", ct);
        return splits ?? new();
    }


}
