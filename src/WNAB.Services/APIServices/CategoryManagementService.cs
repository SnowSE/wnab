using System.Net.Http.Json;
using WNAB.Data;

namespace WNAB.Services;

/// <summary>
/// Handles category-related operations via the API.
/// </summary>
public class CategoryManagementService
{
    private readonly HttpClient _http;

 
    public CategoryManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<int> CreateCategoryAsync(CategoryRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("categories", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<Category>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating category.");
        return created.Id;
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var items = await _http.GetFromJsonAsync<List<Category>>("all/categories", ct);
        return items ?? new();
    }

    public async Task<List<Category>> GetCategoriesForUserAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<Category>>("categories", ct);
        return list ?? new();
    }
}
