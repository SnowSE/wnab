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
        var snapshot = await budgetSnapshotService.GetSnapshotAsync(month, year);

        if (snapshot is null || !snapshot.IsValid)
        {
            snapshot = await RebuildSnapshots(month, year);
        }

        // Calculate from snapshot
        var futureAllocations = await categoryAllocationManagementService.GetAllFutureAllocationsAsync(month, year);
        var overspend = snapshot.Categories
            .Where(c => c.Available < 0)
            .Sum(c => Math.Abs(c.Available));

        return snapshot.SnapshotReadyToAssign - futureAllocations.Sum(f => f.BudgetedAmount);
    }

    public async Task<BudgetSnapshot> RebuildSnapshots(int targetMonth, int targetYear)
    {
        // Build the snapshot for the target month
        BudgetSnapshot snapshot;
        
        // Check if this is the first snapshot (at earliest activity date)
        var earliestDate = await userService.GetEarliestActivityDate();
        if (targetMonth == earliestDate.Month && targetYear == earliestDate.Year)
        {
            snapshot = await CreateFirstSnapshot();
        }
        else
        {
            // Need to get the previous month's snapshot first
            var (prevMonth, prevYear) = CalculatePreviousMonth(targetMonth, targetYear);
            
            // Check if previous snapshot exists, if not rebuild it
            var previousSnapshot = await budgetSnapshotService.GetSnapshotAsync(prevMonth, prevYear);
            if (previousSnapshot == null)
            {
                previousSnapshot = await RebuildSnapshots(prevMonth, prevYear);
            }
            
            snapshot = await CreateNextSnapshot(previousSnapshot);
        }

        // Save the newly created/rebuilt snapshot via API
        await budgetSnapshotService.SaveSnapshotAsync(snapshot);

        return snapshot;
    }

    public async Task<BudgetSnapshot> CreateFirstSnapshot()
    {
        // make it the earliest transaction date. 
        var accountCreationDate = await userService.GetEarliestActivityDate();
        var currentMonth = accountCreationDate.Month;
        var currentYear = accountCreationDate.Year;

        var income = await GetIncomeForMonth(currentMonth, currentYear);
        var allocations = await GetAllocationsForMonth(currentMonth, currentYear);
        var categoryData = await BuildCategorySnapshotData(currentMonth, currentYear);

        var userId = await userService.GetUserId();

        return new BudgetSnapshot
        {
            Month = currentMonth,
            Year = currentYear,
            SnapshotReadyToAssign = income - allocations,
            Categories = categoryData,
            UserId = userId
        };
    }

    public async Task<BudgetSnapshot> CreateNextSnapshot(BudgetSnapshot previousSnapshot)
    {
        var (currentMonth, currentYear) = CalculateNextMonth(previousSnapshot.Month, previousSnapshot.Year);

        var income = await GetIncomeForMonth(currentMonth, currentYear);
        var allocations = await GetAllocationsForMonth(currentMonth, currentYear);
        var overspend = CalculateOverspend(previousSnapshot);
        var categoryData = await BuildCategorySnapshotData(currentMonth, currentYear);

        return new BudgetSnapshot
        {
            Month = currentMonth,
            Year = currentYear,
            SnapshotReadyToAssign = previousSnapshot.SnapshotReadyToAssign + income - allocations - overspend,
            Categories = categoryData
        };
    }

    public (int month, int year) CalculateNextMonth(int currentMonth, int currentYear)
    {
        var nextMonth = currentMonth + 1;
        var nextYear = currentYear;

        if (nextMonth > 12)
        {
            nextMonth = 1;
            nextYear++;
        }

        return (nextMonth, nextYear);
    }

    public (int month, int year) CalculatePreviousMonth(int currentMonth, int currentYear)
    {
        var prevMonth = currentMonth - 1;
        var prevYear = currentYear;

        if (prevMonth < 1)
        {
            prevMonth = 12;
            prevYear--;
        }

        return (prevMonth, prevYear);
    }

    public decimal CalculateOverspend(BudgetSnapshot snapshot)
    {
        return snapshot.Categories
            .Where(c => c.Available < 0)
            .Sum(c => Math.Abs(c.Available));
    }

    public bool ShouldContinueBuilding(BudgetSnapshot snapshot, int targetMonth, int targetYear)
    {
        return snapshot.Year < targetYear || (snapshot.Year == targetYear && snapshot.Month < targetMonth);
    }

    public async Task<decimal> GetIncomeForMonth(int month, int year)
    {
        var allTransactions = await transactionManagementService.GetTransactionSplitsByMonthAsync(new DateTime(year, month, 1));
        return allTransactions
            .Where(t => t.CategoryAllocationId is null)
            .Sum(t => t.Amount);
    }

    public async Task<decimal> GetAllocationsForMonth(int month, int year)
    {
        var allAllocations = await categoryAllocationManagementService.GetAllAllocationsAsync();
        return allAllocations
            .Where(a => a.Month == month && a.Year == year)
            .Sum(a => a.BudgetedAmount);
    }

    public async Task<List<CategorySnapshotData>> BuildCategorySnapshotData(int month, int year)
    {
        var allAllocations = await categoryAllocationManagementService.GetAllAllocationsAsync();
        var categoryData = new List<CategorySnapshotData>();

        var uniqueCategories = allAllocations
            .Where(a => a.Year < year || (a.Year == year && a.Month <= month))
            .Select(a => a.CategoryId)
            .Distinct();

        foreach (var categoryId in uniqueCategories)
        {
            var categoryAllocations = allAllocations
                .Where(a => a.CategoryId == categoryId && (a.Year < year || (a.Year == year && a.Month <= month)))
                .ToList();

            var assignedValue = categoryAllocations
                .Where(a => a.Month == month && a.Year == year)
                .Sum(a => a.BudgetedAmount);

            decimal activity = 0m;
            foreach (var allocation in categoryAllocations)
            {
                var splits = await transactionManagementService.GetTransactionSplitsForAllocationAsync(allocation.Id);
                activity += splits
                    .Where(s => s.CategoryAllocationId == allocation.Id)
                    .Sum(s => s.Amount);
            }

            var totalAllocated = categoryAllocations.Sum(a => a.BudgetedAmount);
            var available = totalAllocated - activity;

            categoryData.Add(new CategorySnapshotData
            {
                CategoryId = categoryId,
                AssignedValue = assignedValue,
                Activity = activity,
                Available = available
            });
        }

        return categoryData;
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
