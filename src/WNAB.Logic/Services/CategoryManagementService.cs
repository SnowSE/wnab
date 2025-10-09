using System.Net.Http.Json;
using WNAB.Logic.Data;

namespace WNAB.Logic;

/// <summary>
/// Handles category-related operations via the API.
/// </summary>
public class CategoryManagementService
{
    private readonly HttpClient _http;

    /// <summary>
    /// Construct with an HttpClient configured with API BaseAddress (e.g. https://localhost:7077/)
    /// </summary>
    public CategoryManagementService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    // LLM-Dev: Mirrored split from UserManagementService: one method builds the DTO, one sends it.
    /// <summary>
    /// Creates a <see cref="CategoryRecord"/> DTO from inputs.
    /// </summary>
    public static CategoryRecord CreateCategoryRecord(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        return new CategoryRecord(name);
    }

    /// <summary>
    /// Sends the provided <see cref="CategoryRecord"/> to the API via POST /categories and returns the created category Id.
    /// </summary>
    public async Task<int> CreateCategoryAsync(CategoryRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var response = await _http.PostAsJsonAsync("categories", record, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<Category>(cancellationToken: ct);
        if (created is null) throw new InvalidOperationException("API returned no content when creating category.");
        return created.Id;
    }

    // LLM-Dev:v2 Add list method so UI components don't need HttpClient.
    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var items = await _http.GetFromJsonAsync<List<Category>>("all/categories", ct);
        return items ?? new();
    }

    // LLM-Dev:v4 Get categories for the current authenticated user
    public async Task<List<Category>> GetCategoriesForUserAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<List<Category>>("categories", ct);
        return list ?? new();
    }
}
