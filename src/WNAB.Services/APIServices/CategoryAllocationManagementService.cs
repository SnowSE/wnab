using System.Net.Http.Json;
using WNAB.Data;

namespace WNAB.Services;


public class CategoryAllocationManagementService
{
    private readonly HttpClient _http;


    public CategoryAllocationManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

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

    // LLM-Dev:v2 Find specific allocation by category, month, and year
    /// <summary>
    /// Finds a specific CategoryAllocation for a category in a given month/year.
    /// Returns null if no allocation exists.
    /// </summary>
    public async Task<CategoryAllocation?> FindAllocationAsync(int categoryId, int month, int year, CancellationToken ct = default)
    {
        var allocations = await GetAllocationsForCategoryAsync(categoryId, ct);
        return allocations.FirstOrDefault(a => a.Month == month && a.Year == year && a.IsActive);
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
