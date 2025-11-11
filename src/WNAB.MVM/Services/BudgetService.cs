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

    public class BudgetSnapshot
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal RTA { get; set; }
        public List<CategorySnapshotData> Categories { get; set; } = new();
    }

    public class CategorySnapshotData
    {
        public int CategoryId { get; set; }
        public decimal AssignedValue { get; set; }
        public decimal Activity { get; set; }
        public decimal Available { get; set; }
    }

    public async Task<decimal> CalculateReadyToAssign(int month, int year, BudgetSnapshot? snapshot, DateTime? accountCreationDate)
    {
        if (snapshot != null)
        {
            // Calculate from snapshot
            var currentMonthIncome = await GetIncomeForMonth(month, year);
            var currentMonthAllocations = await GetAllocationsForMonth(month, year);
            var overspend = snapshot.Categories
                .Where(c => c.Available < 0)
                .Sum(c => Math.Abs(c.Available));

            return snapshot.RTA + currentMonthIncome - currentMonthAllocations - overspend;
        }
        else
        {
            // Calculate from beginning of time
            if (accountCreationDate == null)
            {
                throw new ArgumentNullException(nameof(accountCreationDate), "Account creation date is required when snapshot is null");
            }

            var allTransactions = await transactionManagementService.GetTransactionSplitsAsync();
            var allAllocations = await categoryAllocationManagementService.GetAllAllocationsAsync();

            var income = allTransactions
                .Where(t => t.CategoryAllocationId is null)
                .Sum(t => t.Amount);

            var allocations = allAllocations
                .Where(a => a.Year < year || (a.Year == year && a.Month <= month))
                .Sum(a => a.BudgetedAmount);

            return income - allocations;
        }
    }

    public async Task<BudgetSnapshot> RebuildSnapshots(BudgetSnapshot? previousSnapshot, int targetMonth, int targetYear, DateTime? accountCreationDate)
    {
        if (previousSnapshot == null && accountCreationDate == null)
        {
            throw new ArgumentNullException(nameof(accountCreationDate), "Account creation date is required when there is no previous snapshot");
        }

        var snapshot = previousSnapshot == null
            ? await CreateFirstSnapshot(accountCreationDate!.Value)
            : await CreateNextSnapshot(previousSnapshot);

        if (ShouldContinueBuilding(snapshot, targetMonth, targetYear))
        {
            return await RebuildSnapshots(snapshot, targetMonth, targetYear, accountCreationDate);
        }

        return snapshot;
    }

    private async Task<BudgetSnapshot> CreateFirstSnapshot(DateTime accountCreationDate)
    {
        var currentMonth = accountCreationDate.Month;
        var currentYear = accountCreationDate.Year;

        var income = await GetIncomeForMonth(currentMonth, currentYear);
        var allocations = await GetAllocationsForMonth(currentMonth, currentYear);
        var categoryData = await BuildCategorySnapshotData(currentMonth, currentYear);

        return new BudgetSnapshot
        {
            Month = currentMonth,
            Year = currentYear,
            RTA = income - allocations,
            Categories = categoryData
        };
    }

    private async Task<BudgetSnapshot> CreateNextSnapshot(BudgetSnapshot previousSnapshot)
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
            RTA = previousSnapshot.RTA + income - allocations - overspend,
            Categories = categoryData
        };
    }

    private (int month, int year) CalculateNextMonth(int currentMonth, int currentYear)
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

    private decimal CalculateOverspend(BudgetSnapshot snapshot)
    {
        return snapshot.Categories
            .Where(c => c.Available < 0)
            .Sum(c => Math.Abs(c.Available));
    }

    private bool ShouldContinueBuilding(BudgetSnapshot snapshot, int targetMonth, int targetYear)
    {
        return snapshot.Year < targetYear || (snapshot.Year == targetYear && snapshot.Month < targetMonth);
    }

    private async Task<decimal> GetIncomeForMonth(int month, int year)
    {
        var allTransactions = await transactionManagementService.GetTransactionSplitsAsync();
        return allTransactions
            .Where(t => t.CategoryAllocationId is null)
            .Sum(t => t.Amount);
    }

    private async Task<decimal> GetAllocationsForMonth(int month, int year)
    {
        var allAllocations = await categoryAllocationManagementService.GetAllAllocationsAsync();
        return allAllocations
            .Where(a => a.Month == month && a.Year == year)
            .Sum(a => a.BudgetedAmount);
    }

    private async Task<List<CategorySnapshotData>> BuildCategorySnapshotData(int month, int year)
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
