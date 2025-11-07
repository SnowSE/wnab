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

namespace WNAB.Tests.Unit;

public class RTATest
{
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
        var expectedRTA = 500m;
        
        // Mock the ICategoryAllocationManagementService to return our allocations
        var mockAllocationService = Substitute.For<ICategoryAllocationManagementService>();
        mockAllocationService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(allocations));
        
        var mockHttpClient = Substitute.For<HttpClient>();
        var budgetService = new BudgetService(mockHttpClient, mockAllocationService);

        // Act
        var actualRTA = await budgetService.CalculateRTA(10, 2025);

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
            IsIncome = false,
            Description = "Overspending transaction"
        };

        var incomeTransaction = new TransactionSplit
        {
            Id = 2,
            Amount = 300m,  // Income of $50
            IsIncome = true,
            Description = "Income transaction"
        };
        
        allocation1.TransactionSplits.Add(transactionSplit);

        var expectedRTA = -50m; // 300 budgeted - 50 overspent = -50 remaining
        
        // Mock the ICategoryAllocationManagementService to return our allocations
        var mockAllocationService = Substitute.For<ICategoryAllocationManagementService>();
        mockAllocationService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(allocations));
        
        var mockHttpClient = Substitute.For<HttpClient>();
        var budgetService = new BudgetService(mockHttpClient, mockAllocationService);
        
        // Act
        var actualRTA = await budgetService.CalculateRTA(11, 2025);
        
        // Assert
        actualRTA.ShouldBe(expectedRTA);
    }
}
