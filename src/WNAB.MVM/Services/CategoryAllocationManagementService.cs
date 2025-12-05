using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;


public class CategoryAllocationManagementService : ICategoryAllocationManagementService
{
    private readonly HttpClient _http;
    private readonly ILogger<CategoryAllocationManagementService>? _logger;


    public CategoryAllocationManagementService(HttpClient http, ILogger<CategoryAllocationManagementService>? logger = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger;
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
        _logger?.LogInformation("[CategoryAllocationManagementService] FindAllocationAsync called: CategoryId={CategoryId}, Month={Month}, Year={Year}", 
            categoryId, month, year);
        
        var allocations = await GetAllocationsForCategoryAsync(categoryId, ct);
        
        _logger?.LogInformation("[CategoryAllocationManagementService] GetAllocationsForCategoryAsync returned {Count} allocations for category {CategoryId}", 
            allocations.Count, categoryId);
        
        foreach (var a in allocations)
        {
            _logger?.LogDebug("[CategoryAllocationManagementService] Allocation: Id={Id}, Month={Month}, Year={Year}, IsActive={IsActive}", 
                a.Id, a.Month, a.Year, a.IsActive);
        }
        
        var result = allocations.FirstOrDefault(a => a.Month == month && a.Year == year && a.IsActive);
        
        _logger?.LogInformation("[CategoryAllocationManagementService] FindAllocationAsync result: {AllocationId}", result?.Id);
        
        return result;
    }

    /// <summary>
    /// Finds an existing allocation for the category/month/year, or creates a new one with $0 budget if none exists.
    /// This ensures transactions can always be categorized without requiring a pre-existing budget allocation.
    /// </summary>
    public async Task<CategoryAllocation> FindOrCreateAllocationAsync(int categoryId, int month, int year, CancellationToken ct = default)
    {
        _logger?.LogInformation("[CategoryAllocationManagementService] FindOrCreateAllocationAsync called: CategoryId={CategoryId}, Month={Month}, Year={Year}", 
            categoryId, month, year);
        
        // First, try to find an existing allocation
        var existing = await FindAllocationAsync(categoryId, month, year, ct);
        
        if (existing != null)
        {
            _logger?.LogInformation("[CategoryAllocationManagementService] Found existing allocation: Id={AllocationId}", existing.Id);
            return existing;
        }
        
        // No allocation exists, create one with $0 budget
        _logger?.LogInformation("[CategoryAllocationManagementService] No allocation found, creating new $0 allocation for CategoryId={CategoryId}, Month={Month}, Year={Year}", 
            categoryId, month, year);
        
        var record = new CategoryAllocationRecord(
            CategoryId: categoryId,
            BudgetedAmount: 0m,
            Month: month,
            Year: year,
            EditorName: "Auto-created",
            PercentageAllocation: null,
            OldAmount: null,
            EditedMemo: "Auto-created when categorizing a transaction"
        );
        
        var newAllocationId = await CreateCategoryAllocationAsync(record, ct);
        
        _logger?.LogInformation("[CategoryAllocationManagementService] Created new allocation: Id={AllocationId}", newAllocationId);
        
        // Fetch the newly created allocation to return the full object
        var allocations = await GetAllocationsForCategoryAsync(categoryId, ct);
        var newAllocation = allocations.FirstOrDefault(a => a.Id == newAllocationId);
        
        if (newAllocation == null)
        {
            // Fallback: construct a minimal allocation object if fetch fails
            _logger?.LogWarning("[CategoryAllocationManagementService] Could not fetch newly created allocation, constructing minimal object");
            newAllocation = new CategoryAllocation
            {
                Id = newAllocationId,
                CategoryId = categoryId,
                BudgetedAmount = 0m,
                Month = month,
                Year = year,
                IsActive = true
            };
        }
        
        return newAllocation;
    }

    /// <summary>
    /// Updates an existing CategoryAllocation (amount and/or IsActive status).
    /// </summary>
    public async Task UpdateCategoryAllocationAsync(UpdateCategoryAllocationRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var response = await _http.PutAsJsonAsync($"allocations/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record IdResponse(int Id);

    // LLM-Dev: Add method to get allocations for a specific category
    public async Task<List<CategoryAllocation>> GetAllocationsForCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        if (categoryId <= 0) return new();
        var allocations = await _http.GetFromJsonAsync<List<CategoryAllocation>>($"allocations/{categoryId}", ct);
        return allocations ?? new();
    }

    public async Task<List<CategoryAllocation>> GetAllAllocationsAsync(CancellationToken ct = default)
    {
        var allocations = await _http.GetFromJsonAsync<List<CategoryAllocation>>($"allocations", ct);
        return allocations ?? throw new InvalidOperationException("API returned no content when retrieving all allocations.");
    }

    public async Task<IEnumerable<CategoryAllocation>> GetAllFutureAllocationsAsync(int month, int year, CancellationToken ct = default)
    {

        var allocations = await _http.GetFromJsonAsync<IEnumerable<CategoryAllocation>>($"allocations", ct);
        if (allocations is null) throw new NotImplementedException("Null allocations!");
        return allocations.Where(a => a.Year > year || (a.Month > month && a.Year == year));
        
    }
}
