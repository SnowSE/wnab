using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls.Internals;
using WNAB.Data;

namespace WNAB.MVM;

public class BudgetService : IBudgetService
{
    private readonly HttpClient _http;
    private readonly ICategoryAllocationManagementService categoryAllocationManagementService;
    private readonly ITransactionManagementService transactionManagementService;


    public BudgetService(HttpClient http, ICategoryAllocationManagementService categoryAllocationService, ITransactionManagementService transactionManagementService)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        this.categoryAllocationManagementService = categoryAllocationService ?? throw new ArgumentNullException(nameof(categoryAllocationService));
        this.transactionManagementService = transactionManagementService ?? throw new ArgumentNullException(nameof(transactionManagementService));
    }

    public async Task<decimal> CalculateReadyToAssign(int month, int year)
    {
        // pull out things from the context

        // do the calculations for RTA
        var allocations = await categoryAllocationManagementService.GetAllAllocationsAsync();
        var allTransactions = await transactionManagementService.GetTransactionSplitsAsync();
        var income = allTransactions.Where(t => t.CategoryAllocationId is null).Sum(t => t.Amount);

        decimal allocationAmount = 0m;
        
        foreach (var allocation in allocations)
        {
            // Only include allocations from current month forward
            if (allocation.Year > year || (allocation.Year == year && allocation.Month >= month))
            {
                allocationAmount += allocation.BudgetedAmount;
            }
        }

        // Calculate available once per unique category
        decimal available = 0m;
        var uniqueCategories = allocations.Select(a => a.CategoryId).Distinct();
        foreach (var categoryId in uniqueCategories)
        {
            available += await CalculateAvailable(categoryId, month, year);
        }

        if (available < 0)
        {
            return income - allocationAmount + available;
        }

        return income - allocationAmount;
    }

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
