using System.Net.Http.Json;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Handles transaction-related operations via the API.
/// </summary>
public class TransactionManagementService : ITransactionManagementService
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

    public async Task<TransactionResponse?> GetTransactionByIdAsync(int transactionId, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<TransactionResponse>($"transactions/{transactionId}", ct);
            return result;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Update a transaction
    public async Task<TransactionResponse> UpdateTransactionAsync(EditTransactionRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var response = await _http.PutAsJsonAsync($"transactions/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<TransactionResponse>(cancellationToken: ct);
        if (updated is null) throw new InvalidOperationException("API returned no content when updating transaction.");
        return updated;
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

    public async Task<List<TransactionSplitResponse>> GetTransactionSplitsByMonthAsync(DateTime date, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<GetTransactionSplitsResponse>($"transactionsplitsbymonth?year={date.Year}&month={date.Month}", ct);

        return result?.TransactionSplits ?? new();
    }

    public async Task<TransactionSplitResponse?> GetTransactionSplitByIdAsync(int splitId, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<TransactionSplitResponse>($"transactionsplits/{splitId}", ct);
            return result;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Update a transaction split
    public async Task<TransactionSplitResponse> UpdateTransactionSplitAsync(EditTransactionSplitRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var response = await _http.PutAsJsonAsync($"transactionsplits/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<TransactionSplitResponse>(cancellationToken: ct);
        if (updated is null) throw new InvalidOperationException("API returned no content when updating transaction split.");
        return updated;
    }

    // Delete endpoints
    public async Task DeleteTransactionAsync(int transactionId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"transactions/{transactionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTransactionSplitAsync(int transactionSplitId, CancellationToken ct = default)
    {
        System.Diagnostics.Debug.WriteLine($"[TransactionManagementService] Deleting split ID: {transactionSplitId}");
        
        var response = await _http.DeleteAsync($"transactionsplits/{transactionSplitId}", ct);
        
        System.Diagnostics.Debug.WriteLine($"[TransactionManagementService] Delete response status: {response.StatusCode}");
        
        response.EnsureSuccessStatusCode();
        
        System.Diagnostics.Debug.WriteLine($"[TransactionManagementService] Split {transactionSplitId} deleted successfully");
    }
}
