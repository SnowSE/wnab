using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

public interface ICategoryAllocationManagementService
{
    Task<int> CreateCategoryAllocationAsync(CategoryAllocationRecord record, CancellationToken ct = default);
    Task<CategoryAllocation?> FindAllocationAsync(int categoryId, int month, int year, CancellationToken ct = default);
    Task UpdateCategoryAllocationAsync(UpdateCategoryAllocationRequest request, CancellationToken ct = default);
    Task<List<CategoryAllocation>> GetAllocationsForCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<List<CategoryAllocation>> GetAllAllocationsAsync(CancellationToken ct = default);
    Task<IEnumerable<CategoryAllocation>> GetAllFutureAllocationsAsync(int month, int year, CancellationToken ct = default);
}
