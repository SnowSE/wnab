using System.Net.Http.Json;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

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

  public async Task<int> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
  {
    if (request is null) throw new ArgumentNullException(nameof(request));

    var response = await _http.PostAsJsonAsync("categories", request, ct);
    response.EnsureSuccessStatusCode();

    // CHANGE: Receive DTO instead of entity
    var created = await response.Content.ReadFromJsonAsync<CategoryResponse>(cancellationToken: ct);
    if (created is null) throw new InvalidOperationException("API returned no content when creating category.");
    return created.Id;
  }

  public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
  {
    // CHANGE: Receive DTOs and map to entities for backward compatibility
    var dtos = await _http.GetFromJsonAsync<List<CategoryResponse>>("all/categories", ct);
    return dtos?.Select(MapToEntity).ToList() ?? new();
  }

  public async Task<List<Category>> GetCategoriesForUserAsync(CancellationToken ct = default)
  {
    // CHANGE: Receive DTOs and map to entities for backward compatibility
    var dtos = await _http.GetFromJsonAsync<List<CategoryResponse>>("categories", ct);
    return dtos?.Select(MapToEntity).ToList() ?? new();
  }

  public async Task UpdateCategoryAsync(int id, EditCategoryRequest request, CancellationToken ct = default)
  {
    if (request is null) throw new ArgumentNullException(nameof(request));

    var response = await _http.PutAsJsonAsync($"categories/{id}", request, ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task DeleteCategoryAsync(int id, CancellationToken ct = default)
  {
    var response = await _http.DeleteAsync($"categories/{id}", ct);
    response.EnsureSuccessStatusCode();
  }

  // Helper method to map DTO to entity
  private static Category MapToEntity(CategoryResponse dto) => new()
  {
    Id = dto.Id,
    Name = dto.Name,
    Color = dto.Color,
    IsActive = dto.IsActive
    // Note: User and UserId are intentionally not set to avoid circular references
  };
}
