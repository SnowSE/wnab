using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Maui.Controls.Internals;
using WNAB.Data;

namespace WNAB.MVM;

public class BudgetService : IBudgetService
{
    private readonly ICategoryAllocationManagementService categoryAllocationManagementService;
    private readonly ITransactionManagementService transactionManagementService;
    private readonly IUserService userService;
    private readonly IBudgetSnapshotService budgetSnapshotService;

    public BudgetService(ICategoryAllocationManagementService categoryAllocationService, ITransactionManagementService transactionManagementService, IUserService userService, IBudgetSnapshotService budgetSnapshotService)
    {
        this.categoryAllocationManagementService = categoryAllocationService ?? throw new ArgumentNullException(nameof(categoryAllocationService));
        this.transactionManagementService = transactionManagementService ?? throw new ArgumentNullException(nameof(transactionManagementService));
        this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        this.budgetSnapshotService = budgetSnapshotService ?? throw new ArgumentNullException(nameof(budgetSnapshotService));
    }

    public async Task<decimal> CalculateReadyToAssign(int month, int year)
    {
        // Request the snapshot from the API; server will create/rebuild if needed.
        var snapshot = await budgetSnapshotService.GetSnapshotAsync(month, year);
        
        if (snapshot is null)
            throw new InvalidOperationException($"Failed to obtain snapshot for {month}/{year} from server");

        // Calculate from snapshot
        var futureAllocations = await categoryAllocationManagementService.GetAllFutureAllocationsAsync(month, year);
        
        var futureTotal = futureAllocations.Sum(f => f.BudgetedAmount);
        var result = snapshot.SnapshotReadyToAssign - futureTotal;

        return result;
    }

    public async Task<BudgetSnapshot> RebuildSnapshots(int targetMonth, int targetYear)
    {
        // Delegate rebuild to the API. The server will build and persist the snapshot if missing/invalid.
        var snapshot = await budgetSnapshotService.GetSnapshotAsync(targetMonth, targetYear);
        if (snapshot is null)
            throw new InvalidOperationException($"Failed to obtain snapshot for {targetMonth}/{targetYear} from server");

        return snapshot;
    }

    // Snapshot creation logic moved to API; avoid duplicating it client-side.

    public async Task<decimal> CalculateAvailable(int categoryId, int month, int year)
    {
        List<CategoryAllocation> allocations = await categoryAllocationManagementService.GetAllocationsForCategoryAsync(categoryId);

        decimal allocationAmount = 0m;
        foreach (var allocation in allocations)
        {
            if (allocation.Year < year || (allocation.Year == year && allocation.Month <= month))
            {
                allocationAmount += allocation.BudgetedAmount;

                var splits = await transactionManagementService.GetTransactionSplitsForAllocationAsync(allocation.Id);
                allocationAmount -= splits.Sum(ts => ts.Amount);
            }
        }

        return allocationAmount;
    }

}
