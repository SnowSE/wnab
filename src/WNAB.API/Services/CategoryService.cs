using Microsoft.EntityFrameworkCore;
using WNAB.Core.Models;
using WNAB.Core.Services;
using WNAB.Data;

namespace WNAB.API.Services;

public class CategoryService : ICategoryService
{
    private readonly WnabDbContext _context;

    public CategoryService(WnabDbContext context)
    {
        _context = context;
    }

    public async Task<Category> CreateCategoryAsync(int userId, CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Color = request.Color,
            Description = request.Description,
            BudgetAmount = request.BudgetAmount,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return category;
    }

    public async Task<Category?> GetCategoryAsync(int userId, int categoryId)
    {
        return await _context.Categories
            .Include(c => c.Transactions.OrderByDescending(t => t.Date).Take(10))
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync(int userId)
    {
        return await _context.Categories
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> UpdateCategoryAsync(int userId, int categoryId, UpdateCategoryRequest request)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            category.Name = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Color))
            category.Color = request.Color;

        if (request.Description != null)
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

        if (request.BudgetAmount.HasValue)
            category.BudgetAmount = request.BudgetAmount.Value;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int userId, int categoryId)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            return false;
        }

        // Check if category has transactions
        var hasTransactions = await _context.Transactions
            .AnyAsync(t => t.CategoryId == categoryId);

        if (hasTransactions)
        {
            // Soft delete - just mark as inactive
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Hard delete if no transactions
            _context.Categories.Remove(category);
        }

        await _context.SaveChangesAsync();

        return true;
    }
}