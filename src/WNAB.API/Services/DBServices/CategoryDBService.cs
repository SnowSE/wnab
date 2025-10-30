using Microsoft.EntityFrameworkCore;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.API;

public class CategoryDBService
{
    private readonly WnabContext _db;

    public CategoryDBService(WnabContext db)
    {
        _db = db;
    }

    public WnabContext DbContext => _db;

    public async Task<bool> CategoryBelongsToUserAsync(int categoryId, int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsDuplicateCategoryNameAsync(string name, int userId, int? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        return await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == name && c.IsActive && (!excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value), cancellationToken);
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public async Task<Category> CreateCategoryAsync(int userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            Name = request.Name,
            Color = request.Color,
            UserId = userId
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<Category> CreateCategoryWithValidationAsync(int userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // Check for duplicate category name
        var isDuplicate = await IsDuplicateCategoryNameAsync(request.Name, userId, cancellationToken: cancellationToken);
        if (isDuplicate)
        {
            throw new InvalidOperationException("DuplicateCategoryName");
        }

        // Create the category
        return await CreateCategoryAsync(userId, request, cancellationToken);
    }

    public async Task<Category> UpdateCategoryAsync(Category category, EditCategoryRequest request, CancellationToken cancellationToken = default)
    {
        category.Name = request.NewName;
        category.Color = request.NewColor;
        category.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<Category> UpdateCategoryWithValidationAsync(int userId, int categoryId, EditCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // Validate that the category ID belongs to the current user
        var category = await GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category == null || category.UserId != userId)
        {
            throw new InvalidOperationException("InvalidCategoryId");
        }

        // Check for duplicate category name
        var isDuplicate = await IsDuplicateCategoryNameAsync(request.NewName, userId, categoryId, cancellationToken);
        if (isDuplicate)
        {
            throw new InvalidOperationException("DuplicateCategoryName");
        }

        // Update the category
        return await UpdateCategoryAsync(category, request, cancellationToken);
    }

    public async Task DeleteCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryWithValidationAsync(int userId, int categoryId, CancellationToken cancellationToken = default)
    {
        // Validate that the category ID belongs to the current user
        var category = await GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category == null || category.UserId != userId)
        {
            throw new InvalidOperationException("InvalidCategoryId");
        }

        // Delete the category
        await DeleteCategoryAsync(category, cancellationToken);
    }
}