using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

/// <summary>
/// Handles category allocation operations via the API, plus a static factory for DTO creation.
/// </summary>
public class CategoryAllocationManagementService
{
    private readonly HttpClient _http;

    /// <summary>
    /// Construct with an HttpClient configured with API BaseAddress (e.g. https://localhost:7077/)
    /// </summary>
    public CategoryAllocationManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    // LLM-Dev: Static factory for allocation record to unify test creation without HttpClient/service instances.
    public static CategoryAllocationRecord CreateCategoryAllocationRecord(int categoryId, decimal budgetedAmount, int month, int year)
    {
        if (categoryId <= 0) throw new ArgumentOutOfRangeException(nameof(categoryId), "CategoryId must be positive.");
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month), "Month must be 1-12.");
        if (year < 1) throw new ArgumentOutOfRangeException(nameof(year), "Year must be positive.");
        if (budgetedAmount < 0) throw new ArgumentOutOfRangeException(nameof(budgetedAmount), "BudgetedAmount cannot be negative.");
        return new CategoryAllocationRecord(categoryId, budgetedAmount, month, year);
    }

    /// <summary>
    /// Sends the provided CategoryAllocationRecord to the API via POST /allocations and returns the created allocation Id.
    /// </summary>
    public async Task<int> CreateCategoryAllocationAsync(CategoryAllocationRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        // LLM-Dev: POST to REST endpoint that accepts CategoryAllocationRecord and returns new Id
        var response = await _http.PostAsJsonAsync("allocations", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<IdResponse>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating allocation.");
        return created.Id;
    }

    private sealed record IdResponse(int Id);

    // LLM-Dev: Add method to get allocations for a specific category
    public async Task<List<CategoryAllocation>> GetAllocationsForCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        if (categoryId <= 0) return new();
        var allocations = await _http.GetFromJsonAsync<List<CategoryAllocation>>($"allocations?categoryId={categoryId}", ct);
        return allocations ?? new();
    }
}
