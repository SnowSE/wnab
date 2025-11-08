using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Controls.Internals;
using WNAB.Data;

namespace WNAB.MVM;

public class BudgetService : IBudgetService
{
    private readonly HttpClient _http;
    private readonly ICategoryAllocationManagementService categoryAllocationService;
    private readonly ITransactionManagementService transactionManagementService;


    public BudgetService(HttpClient http, ICategoryAllocationManagementService categoryAllocationService, ITransactionManagementService transactionManagementService)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        this.categoryAllocationService = categoryAllocationService ?? throw new ArgumentNullException(nameof(categoryAllocationService));
        this.transactionManagementService = transactionManagementService ?? throw new ArgumentNullException(nameof(transactionManagementService));
    }

    public async Task<decimal> CalculateReadyToAssign(int month, int year)
    {
        // pull out things from the context

        // do the calculations for RTA
        var allocations = await categoryAllocationService.GetAllAllocationsAsync();
        var allTransactions = await transactionManagementService.GetTransactionSplitsAsync();
        var income = allTransactions.Where(t => t.CategoryAllocationId is null).Sum(t => t.Amount);

        decimal allocationAmount = 0m;
        foreach (var allocation in allocations)
        {
            allocationAmount += allocation.BudgetedAmount;
        }
        return income - allocationAmount;
    }
}
