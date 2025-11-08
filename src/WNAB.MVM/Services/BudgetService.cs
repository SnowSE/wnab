using System;
using System.Collections.Generic;
using System.Text;
using WNAB.Data;

namespace WNAB.MVM;

public class BudgetService
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
        decimal rta = 0m;
        foreach (var allocation in allocations)
        {
            rta += allocation.BudgetedAmount;
        }
        return rta;
    }
}
