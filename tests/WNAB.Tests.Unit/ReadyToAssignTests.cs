using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using WNAB.Data;
using WNAB.MVM;
using WNAB.SharedDTOs;

namespace WNAB.Tests.Unit;

public class ReadyToAssignTests
{

    private ICategoryAllocationManagementService _categoryAllocationManagementService;
    private ITransactionManagementService _transactionManagementService;
    private HttpClient _httpClient;
    private IBudgetService _budgetService;

    public ReadyToAssignTests()
    {
        _categoryAllocationManagementService = Substitute.For<ICategoryAllocationManagementService>();
        _transactionManagementService = Substitute.For<ITransactionManagementService>();
        _httpClient = Substitute.For<HttpClient>();
        _budgetService = new BudgetService(_httpClient, _categoryAllocationManagementService, _transactionManagementService);
    }

    [Fact]
    public async Task CalculateRTA_GivenAllocations()
    {
        // Arrange
        var allocations = new List<CategoryAllocation>
        {
            new CategoryAllocation { CategoryId = 1, BudgetedAmount = 100m, Month = 10, Year = 2025 },
            new CategoryAllocation { CategoryId = 2, BudgetedAmount = 250m, Month = 10, Year = 2025 },
            new CategoryAllocation { CategoryId = 3, BudgetedAmount = 150m, Month = 10, Year = 2025 }
        };

        var transactionSplit = new TransactionSplit
        {
            Id = 1,
            CategoryAllocationId = null,
            Amount = 500m,
            Description = "Income transaction"
        };

        _categoryAllocationManagementService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(allocations));

        _transactionManagementService.GetTransactionSplitsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<TransactionSplitResponse>
            {
                new TransactionSplitResponse(
                    Id: transactionSplit.Id,
                    CategoryName: "Income",
                    CategoryAllocationId: transactionSplit.CategoryAllocationId,
                    TransactionId: 1,
                    Amount: transactionSplit.Amount,
                    Description: transactionSplit.Description
                )
            }));

        var expectedRTA = 0m;

        // Act
        var actualRTA = await _budgetService.CalculateReadyToAssign(10, 2025);

        // Assert
        actualRTA.ShouldBe(expectedRTA);
    }

    [Fact]
    public async Task CalculateRTA_GivenOverspending()
    {
        // Arrange
        var allocation1 = new CategoryAllocation
        {
            Id = 1,
            CategoryId = 1,
            BudgetedAmount = 100m,
            Month = 11,
            Year = 2025
        };

        var allocation2 = new CategoryAllocation
        {
            Id = 2,
            CategoryId = 2,
            BudgetedAmount = 200m,
            Month = 11,
            Year = 2025
        };

        var allocations = new List<CategoryAllocation> { allocation1, allocation2 };

        // Transaction split that overspends allocation1 by $50
        var transactionSplit = new TransactionSplit
        {
            Id = 1,
            CategoryAllocationId = 1,
            Amount = 150m,  // Spending $150 when only $100 is budgeted
            Description = "Overspending transaction"
        };

        var incomeTransaction = new TransactionSplit
        {
            Id = 2,
            Amount = 300m,  // Income of $50
            Description = "Income transaction"
        };

        allocation1.TransactionSplits.Add(transactionSplit);

        var expectedRTA = -50m; // 300 budgeted - 50 overspent = -50 remaining

        // Mock the ICategoryAllocationManagementService to return our allocations
        var mockAllocationService = Substitute.For<ICategoryAllocationManagementService>();
        mockAllocationService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(allocations));

        var mockTransactionService = Substitute.For<ITransactionManagementService>();
        var mockHttpClient = Substitute.For<HttpClient>();
        var budgetService = new BudgetService(mockHttpClient, mockAllocationService, mockTransactionService);

        // Act
        var actualRTA = await budgetService.CalculateReadyToAssign(11, 2025);

        // Assert
        actualRTA.ShouldBe(expectedRTA);
    }
}
