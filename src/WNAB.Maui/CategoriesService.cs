using System.Net.Http.Json;

namespace WNAB.Maui;

public interface ICategoriesService
{
    Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken ct = default);
}

public sealed class CategoriesService : ICategoriesService
{
    private readonly HttpClient _http;

    public CategoriesService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<List<CategoryDto>>("categories", ct) ?? new();
        return resp.Select(c => new CategoryItem(c.Id, c.Name ?? string.Empty)).ToList();
    }

    private sealed class CategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}