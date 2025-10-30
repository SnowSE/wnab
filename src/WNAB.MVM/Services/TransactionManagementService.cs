using System.Net.Http.Json;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Handles transaction-related operations via the API.
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

        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating transaction.");
        return created.Id;
    }

    // Get transactions for a specific account (uses unified /transactions endpoint with accountId filter)
    public async Task<List<TransactionResponse>> GetTransactionsForAccountAsync(int accountId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<GetTransactionsResponse>($"transactions?accountId={accountId}", ct);
        return result?.Transactions ?? new();
    }

    // Get transactions (optionally filtered by accountId)
    public async Task<List<TransactionResponse>> GetTransactionsAsync(int? accountId = null, CancellationToken ct = default)
    {
        var url = accountId.HasValue ? $"transactions?accountId={accountId.Value}" : "transactions";
        var result = await _http.GetFromJsonAsync<GetTransactionsResponse>(url, ct);
        return result?.Transactions ?? new();
    }

    /// <summary>
    /// Gets all transactions for the current authenticated user across all their accounts.
    /// Uses API response record types.
    /// </summary>
    public async Task<List<TransactionResponse>> GetTransactionsForUserAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<GetTransactionsResponse>("transactions", ct);
        return result?.Transactions ?? new();
    }

    // Create a transaction split
    public async Task<int> CreateTransactionSplitAsync(TransactionSplitRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("transactionsplits", record, ct);
        response.EnsureSuccessStatusCode();

        // Assuming API returns created split entity; if it returns a DTO with Id, adjust accordingly
        var created = await response.Content.ReadFromJsonAsync<TransactionSplitResponse>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating transaction split.");
        return created.Id;
    }

    // Get splits for a specific allocation (separate from transactions)
    public async Task<List<TransactionSplitResponse>> GetTransactionSplitsForAllocationAsync(int allocationId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<GetTransactionSplitsResponse>($"transactionsplits?allocationId={allocationId}", ct);
        return result?.TransactionSplits ?? new();
    }

    // New: Get all transaction splits for the current user
    public async Task<List<TransactionSplitResponse>> GetTransactionSplitsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<GetTransactionSplitsResponse>("transactionsplits", ct);
        return result?.TransactionSplits ?? new();
    }

    // Delete endpoints
    public async Task DeleteTransactionAsync(int transactionId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"transactions/{transactionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTransactionSplitAsync(int transactionSplitId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"transactionsplits/{transactionSplitId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
