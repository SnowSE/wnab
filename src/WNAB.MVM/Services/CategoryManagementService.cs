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

  public async Task<int> CreateCategoryAsync(CategoryRecord record, CancellationToken ct = default)
  {
    if (record is null) throw new ArgumentNullException(nameof(record));

    var response = await _http.PostAsJsonAsync("categories", record, ct);
    response.EnsureSuccessStatusCode();

    // CHANGE: Receive DTO instead of entity
    var created = await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct);
    if (created is null) throw new InvalidOperationException("API returned no content when creating category.");
    return created.Id;
  }

  public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
  {
    // CHANGE: Receive DTOs and map to entities for backward compatibility
    var dtos = await _http.GetFromJsonAsync<List<CategoryDto>>("all/categories", ct);
    return dtos?.Select(MapToEntity).ToList() ?? new();
  }

  public async Task<List<Category>> GetCategoriesForUserAsync(CancellationToken ct = default)
  {
    // CHANGE: Receive DTOs and map to entities for backward compatibility
    var dtos = await _http.GetFromJsonAsync<List<CategoryDto>>("categories", ct);
    return dtos?.Select(MapToEntity).ToList() ?? new();
  }

  // Helper method to map DTO to entity
  private static Category MapToEntity(CategoryDto dto) => new()
  {
    Id = dto.Id,
    Name = dto.Name,
    Color = dto.Color,
    IsActive = dto.IsActive
    // Note: User and UserId are intentionally not set to avoid circular references
  };
}
