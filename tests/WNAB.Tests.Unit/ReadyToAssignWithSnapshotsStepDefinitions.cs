using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Reqnroll;
using Shouldly;
using WNAB.Data;
using WNAB.MVM;
using WNAB.SharedDTOs;
using static WNAB.MVM.BudgetService;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
    // prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
    // Rule: Use the services where possible.
    // Rule: functions may only have DataTable as a parameter or no parameter.

    [Given(@"the account was created on (.*)")]
    public void GivenTheAccountWasCreatedOn(string dateString)
    {
        var accountCreationDate = DateTime.Parse(dateString);
        context["AccountCreationDate"] = accountCreationDate;
    }

    [Given(@"the following income transactions exist")]
    public void GivenTheFollowingIncomeTransactionsExist(DataTable dataTable)
    {
        var transactionSplits = context.ContainsKey("TransactionSplits") 
            ? context.Get<List<TransactionSplit>>("TransactionSplits") 
            : new List<TransactionSplit>();
        
        var transactionManagementService = GetOrCreateTransactionManagementService();

        foreach (var row in dataTable.Rows)
        {
            var transactionSplit = new TransactionSplit
            {
                Id = transactionSplits.Count + 1,
                CategoryAllocationId = null,
                Amount = decimal.Parse(row["Amount"]),
                Description = row["Description"]
            };
            transactionSplits.Add(transactionSplit);
        }

        context["TransactionSplits"] = transactionSplits;
        SetupTransactionMocks(transactionSplits, transactionManagementService);
    }

    [Given(@"the following category allocations exist")]
    public void GivenTheFollowingCategoryAllocationsExist(DataTable dataTable)
    {
        var allocations = context.ContainsKey("CategoryAllocationsForSnapshot") 
            ? context.Get<List<CategoryAllocation>>("CategoryAllocationsForSnapshot") 
            : new List<CategoryAllocation>();
        
        var categoryAllocationManagementService = GetOrCreateCategoryAllocationService();

        foreach (var row in dataTable.Rows)
        {
            var date = DateTime.Parse(row["Date"]);
            
            var allocation = new CategoryAllocation
            {
                Id = allocations.Count + 1,
                CategoryId = int.Parse(row["CategoryId"]),
                BudgetedAmount = decimal.Parse(row["BudgetedAmount"]),
                Month = date.Month,
                Year = date.Year
            };
            allocations.Add(allocation);
        }

        context["CategoryAllocationsForSnapshot"] = allocations;
        SetupAllocationMocks(allocations, categoryAllocationManagementService);
    }

    [Given(@"the following spending exists")]
    public void GivenTheFollowingSpendingExists(DataTable dataTable)
    {
        var allocations = context.Get<List<CategoryAllocation>>("CategoryAllocationsForSnapshot");
        var transactionSplits = context.ContainsKey("TransactionSplits") 
            ? context.Get<List<TransactionSplit>>("TransactionSplits") 
            : new List<TransactionSplit>();
        
        var transactionManagementService = GetOrCreateTransactionManagementService();

        foreach (var row in dataTable.Rows)
        {
            var date = DateTime.Parse(row["Date"]);
            var categoryId = int.Parse(row["CategoryId"]);
            var allocation = allocations.FirstOrDefault(a => 
                a.CategoryId == categoryId && 
                a.Month == date.Month && 
                a.Year == date.Year);

            var transactionSplit = new TransactionSplit
            {
                Id = transactionSplits.Count + 1,
                CategoryAllocationId = allocation?.Id,
                Amount = decimal.Parse(row["Amount"]),
                Description = row["Description"]
            };
            transactionSplits.Add(transactionSplit);
        }

        context["TransactionSplits"] = transactionSplits;
        SetupTransactionMocks(transactionSplits, transactionManagementService);
    }

    [Given(@"I have a previous snapshot with the following details")]
    public void GivenIHaveAPreviousSnapshotWithTheFollowingDetails(DataTable dataTable)
    {
        var row = dataTable.Rows[0];
        var month = int.Parse(row["Month"]);
        var year = int.Parse(row["Year"]);
        var rta = decimal.Parse(row["RTA"]);
        
        var previousSnapshot = new BudgetSnapshot
        {
            Month = month,
            Year = year,
            RTA = rta,
            Categories = new List<CategorySnapshotData>()
        };
        context["PreviousSnapshot"] = previousSnapshot;
    }

    [Given(@"the previous snapshot has the following categories")]
    public void GivenThePreviousSnapshotHasTheFollowingCategories(DataTable dataTable)
    {
        var previousSnapshot = context.Get<BudgetSnapshot>("PreviousSnapshot");
        
        if (previousSnapshot == null)
        {
            throw new InvalidOperationException("Previous snapshot must be initialized first");
        }

        foreach (var row in dataTable.Rows)
        {
            previousSnapshot.Categories.Add(new CategorySnapshotData
            {
                CategoryId = int.Parse(row["CategoryId"]),
                AssignedValue = decimal.Parse(row["Assigned"]),
                Activity = decimal.Parse(row["Activity"]),
                Available = decimal.Parse(row["Available"])
            });
        }
    }

    [When(@"I rebuild snapshots to (.*) (.*)")]
    public async Task WhenIRebuildSnapshotsTo(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var accountCreationDate = context.ContainsKey("AccountCreationDate") 
            ? context.Get<DateTime>("AccountCreationDate")
            : new DateTime(year, month, 1); // Default to first day of the month if not specified
        var budgetService = GetOrCreateBudgetService();
        
        var resultSnapshot = await budgetService.RebuildSnapshots(null, month, year, accountCreationDate);
        context["ResultSnapshot"] = resultSnapshot;
    }

    [When(@"I build snapshot from (.*) to (.*) (.*)")]
    public async Task WhenIBuildSnapshotFromTo(string fromMonthName, string toMonthName, int year)
    {
        var toMonth = DateTime.Parse($"1 {toMonthName} {year}").Month;
        var previousSnapshot = context.Get<BudgetSnapshot>("PreviousSnapshot");
        var budgetService = GetOrCreateBudgetService();
        
        var resultSnapshot = await budgetService.RebuildSnapshots(previousSnapshot, toMonth, year, null);
        context["ResultSnapshot"] = resultSnapshot;
    }

    [When(@"I calculate RTA for (.*) (.*) with the snapshot")]
    public async Task WhenICalculateRTAWithTheSnapshot(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var previousSnapshot = context.Get<BudgetSnapshot>("PreviousSnapshot");
        var accountCreationDate = context.ContainsKey("AccountCreationDate") 
            ? context.Get<DateTime>("AccountCreationDate")
            : new DateTime(previousSnapshot.Year, previousSnapshot.Month, 1); // Default to snapshot's month if not specified
        var budgetService = GetOrCreateBudgetService();
        
        var actualRTA = await budgetService.CalculateReadyToAssign(month, year, previousSnapshot, accountCreationDate);
        context["ActualRTA"] = actualRTA;
    }

    [When(@"I calculate RTA for (.*) (.*) without a snapshot")]
    public async Task WhenICalculateRTAWithoutSnapshot(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var accountCreationDate = context.ContainsKey("AccountCreationDate") 
            ? context.Get<DateTime>("AccountCreationDate")
            : new DateTime(year, month, 1); // Default to first day of the month if not specified
        var budgetService = GetOrCreateBudgetService();
        
        var actualRTA = await budgetService.CalculateReadyToAssign(month, year, null, accountCreationDate);
        context["ActualRTA"] = actualRTA;
    }

    [Then(@"the snapshot for (.*) (.*) should have RTA of (.*)")]
    public void ThenTheSnapshotShouldHaveRTA(string monthName, int year, decimal expectedRTA)
    {
        var resultSnapshot = context.Get<BudgetSnapshot>("ResultSnapshot");
        resultSnapshot.ShouldNotBeNull();
        resultSnapshot.RTA.ShouldBe(expectedRTA);
    }

    [Then(@"the snapshot should have the following categories")]
    public void ThenTheSnapshotShouldHaveTheFollowingCategories(DataTable dataTable)
    {
        var resultSnapshot = context.Get<BudgetSnapshot>("ResultSnapshot");
        resultSnapshot.ShouldNotBeNull();
        
        foreach (var row in dataTable.Rows)
        {
            var categoryId = int.Parse(row["CategoryId"]);
            var assigned = decimal.Parse(row["Assigned"]);
            var activity = decimal.Parse(row["Activity"]);
            var available = decimal.Parse(row["Available"]);
            
            var category = resultSnapshot.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
            category.ShouldNotBeNull();
            category.AssignedValue.ShouldBe(assigned);
            category.Activity.ShouldBe(activity);
            category.Available.ShouldBe(available);
        }
    }

    [Then(@"the RTA should be (.*)")]
    public void ThenTheRTAShouldBe(decimal expectedRTA)
    {
        var actualRTA = context.Get<decimal>("ActualRTA");
        actualRTA.ShouldBe(expectedRTA);
    }

    // Helper methods for service management
    private ICategoryAllocationManagementService GetOrCreateCategoryAllocationService()
    {
        if (!context.ContainsKey("CategoryAllocationManagementService"))
        {
            var service = Substitute.For<ICategoryAllocationManagementService>();
            context["CategoryAllocationManagementService"] = service;
        }
        return context.Get<ICategoryAllocationManagementService>("CategoryAllocationManagementService");
    }

    private ITransactionManagementService GetOrCreateTransactionManagementService()
    {
        if (!context.ContainsKey("TransactionManagementService"))
        {
            var service = Substitute.For<ITransactionManagementService>();
            context["TransactionManagementService"] = service;
        }
        return context.Get<ITransactionManagementService>("TransactionManagementService");
    }

    private BudgetService GetOrCreateBudgetService()
    {
        if (!context.ContainsKey("BudgetService"))
        {
            var httpClient = Substitute.For<HttpClient>();
            var categoryAllocationService = GetOrCreateCategoryAllocationService();
            var transactionService = GetOrCreateTransactionManagementService();
            var service = new BudgetService(httpClient, categoryAllocationService, transactionService);
            context["BudgetService"] = service;
        }
        return context.Get<BudgetService>("BudgetService");
    }

    private void SetupAllocationMocks(List<CategoryAllocation> allocations, ICategoryAllocationManagementService service)
    {
        service.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(allocations));

        service.GetAllocationsForCategoryAsync(Arg.Any<int>())
            .Returns(callInfo =>
            {
                var categoryId = callInfo.Arg<int>();
                var categoryAllocations = allocations.Where(a => a.CategoryId == categoryId).ToList();
                return Task.FromResult(categoryAllocations);
            });
    }

    private void SetupTransactionMocks(List<TransactionSplit> transactionSplits, ITransactionManagementService service)
    {
        var transactionSplitResponses = transactionSplits.Select(ts =>
            new TransactionSplitResponse(
                Id: ts.Id,
                CategoryAllocationId: ts.CategoryAllocationId,
                TransactionId: 1,
                CategoryName: ts.CategoryAllocationId.HasValue ? "Some Category" : "Income",
                Amount: ts.Amount,
                Description: ts.Description
            )).ToList();

        service.GetTransactionSplitsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(transactionSplitResponses));

        service.GetTransactionSplitsForAllocationAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var allocationId = callInfo.Arg<int>();
                var matchingSplits = transactionSplits
                    .Where(ts => ts.CategoryAllocationId == allocationId)
                    .Select(ts => new TransactionSplitResponse(
                        Id: ts.Id,
                        CategoryAllocationId: ts.CategoryAllocationId,
                        TransactionId: 1,
                        CategoryName: "Some Category",
                        Amount: ts.Amount,
                        Description: ts.Description
                    ))
                    .ToList();
                return Task.FromResult(matchingSplits);
            });
    }
}
