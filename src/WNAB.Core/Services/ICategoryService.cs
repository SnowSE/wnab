using WNAB.Core.Models;

namespace WNAB.Core.Services;

public interface ICategoryService
{
    Task<Category> CreateCategoryAsync(int userId, CreateCategoryRequest request);
    Task<Category?> GetCategoryAsync(int userId, int categoryId);
    Task<IEnumerable<Category>> GetCategoriesAsync(int userId);
    Task<Category?> UpdateCategoryAsync(int userId, int categoryId, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int userId, int categoryId);
}

public class CreateCategoryRequest
{
    public required string Name { get; set; }
    public string Color { get; set; } = "#007bff";
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public decimal? BudgetAmount { get; set; }
    public bool? IsActive { get; set; }
}