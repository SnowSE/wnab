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
        var allocations = new List<CategoryAllocation>
        {
            new CategoryAllocation { CategoryId = 1, BudgetedAmount = 100m, Month = 10, Year = 2025 },
            new CategoryAllocation { CategoryId = 1, BudgetedAmount = 250m, Month = 11, Year = 2025 },
            new CategoryAllocation { CategoryId = 1, BudgetedAmount = 150m, Month = 12, Year = 2025 }
        };

        var transactionSplits = new List<TransactionSplit>
        {
            new TransactionSplit { Id = 1, CategoryAllocationId = 1, Amount = 150m, Description = "Overspending transaction" },
            new TransactionSplit { Id = 2, CategoryAllocationId = null, Amount = 300m, Description = "Income transaction" }
        };


        _categoryAllocationManagementService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(allocations));
        _categoryAllocationManagementService.GetAllocationsForCategoryAsync(Arg.Any<int>())
            .Returns(Task.FromResult(allocations));

        _transactionManagementService.GetTransactionSplitsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(transactionSplits.ToList().ConvertAll(ts =>
                new TransactionSplitResponse(
                    Id: ts.Id,
                    CategoryAllocationId: ts.CategoryAllocationId,
                    TransactionId: 1,
                    CategoryName: ts.CategoryAllocationId.HasValue ? "Some Category" : "Income",
                    Amount: ts.Amount,
                    Description: ts.Description
                ))));
        _transactionManagementService.GetTransactionSplitsForAllocationAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<TransactionSplitResponse> { new TransactionSplitResponse(
                    Id: transactionSplits.First().Id,
                    CategoryAllocationId: transactionSplits.First().CategoryAllocationId,
                    TransactionId: 1,
                    CategoryName: transactionSplits.First().CategoryAllocationId.HasValue ? "Some Category" : "Income",
                    Amount: transactionSplits.First().Amount,
                    Description: transactionSplits.First().Description
                )
            }));


        var expectedRTA = -250m;

        // Act
        var actualRTA = await _budgetService.CalculateReadyToAssign(10, 2025);

        // Assert
        actualRTA.ShouldBe(expectedRTA);
    }
}
