using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Reqnroll;
using Shouldly;
using WNAB.API;
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
        
        var userService = GetOrCreateUserService();
        SetupUserMocks(accountCreationDate, userService);
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
                Description = row["Description"],
                Transaction = new Transaction
                {
                    TransactionDate = DateTime.Parse(row["Date"])
                }
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
            SnapshotReadyToAssign = rta,
            Categories = new List<CategorySnapshotData>()
        };
        
        // Ensure the snapshot store exists by getting/creating the service first
        GetOrCreateBudgetSnapshotService();
        
        // Store in snapshot store for the mock
        var snapshotStore = context.Get<Dictionary<(int month, int year), BudgetSnapshot>>("SnapshotStore");
        snapshotStore[(month, year)] = previousSnapshot;
    }

    [Given(@"the previous snapshot has the following categories")]
    public void GivenThePreviousSnapshotHasTheFollowingCategories(DataTable dataTable)
    {
        // Ensure the snapshot store exists
        GetOrCreateBudgetSnapshotService();
        
        // Get the snapshot from the store
        var snapshotStore = context.Get<Dictionary<(int month, int year), BudgetSnapshot>>("SnapshotStore");
        var previousSnapshot = snapshotStore.Values.LastOrDefault();

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

    [Then("the income for {DateTime} should be {float}")]
    public async Task ThenTheIncomeForOctoberShouldBe(DateTime date, Decimal expectedIncome)
    {
        var budgetService = GetOrCreateBudgetService();
        var actualIncome = await budgetService.GetIncomeForMonth(date.Month, date.Year);
        actualIncome.ShouldBe(expectedIncome);
    }


    [When(@"I rebuild snapshots to (.*) (.*)")]
    public async Task WhenIRebuildSnapshotsTo(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var accountCreationDate = context.ContainsKey("AccountCreationDate")
            ? context.Get<DateTime>("AccountCreationDate")
            : new DateTime(year, month, 1); // Default to first day of the month if not specified
        var budgetService = GetOrCreateBudgetService();

        var resultSnapshot = await budgetService.RebuildSnapshots(month, year);
        context["ResultSnapshot"] = resultSnapshot;
    }

    [When(@"I build snapshot from (.*) to (.*) (.*)")]
    public async Task WhenIBuildSnapshotFromTo(string fromMonthName, string toMonthName, int year)
    {
        var toMonth = DateTime.Parse($"1 {toMonthName} {year}").Month;
        var budgetService = GetOrCreateBudgetService();

        var resultSnapshot = await budgetService.RebuildSnapshots(toMonth, year);
        context["ResultSnapshot"] = resultSnapshot;
    }

    [When(@"I calculate RTA for (.*) (.*) with the snapshot")]
    public async Task WhenICalculateRTAWithTheSnapshot(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var budgetService = GetOrCreateBudgetService();

        var actualRTA = await budgetService.CalculateReadyToAssign(month, year);
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

        var actualRTA = await budgetService.CalculateReadyToAssign(month, year);
        context["ActualRTA"] = actualRTA;
    }

    [Then(@"the snapshot for (.*) (.*) should have RTA of (.*)")]
    public void ThenTheSnapshotShouldHaveRTA(string monthName, int year, decimal expectedRTA)
    {
        var resultSnapshot = context.Get<BudgetSnapshot>("ResultSnapshot");
        resultSnapshot.ShouldNotBeNull();
        resultSnapshot.SnapshotReadyToAssign.ShouldBe(expectedRTA);
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

    private IUserService GetOrCreateUserService()
    {
        if (!context.ContainsKey("UserService"))
        {
            var service = Substitute.For<IUserService>();
            
            // Setup default earliest activity date if not specified
            var defaultDate = new DateTime(2024, 1, 1);
            service.GetEarliestActivityDate()
                .Returns(Task.FromResult(defaultDate));
            
            context["UserService"] = service;
        }
        return context.Get<IUserService>("UserService");
    }


    private BudgetService GetOrCreateBudgetService()
    {
        if (!context.ContainsKey("BudgetService"))
        {
            var categoryAllocationService = GetOrCreateCategoryAllocationService();
            var transactionService = GetOrCreateTransactionManagementService();
            var userService = GetOrCreateUserService();
            var budgetSnapshotService = GetOrCreateBudgetSnapshotService();
            
            var service = new BudgetService(categoryAllocationService, transactionService, userService, budgetSnapshotService);
            context["BudgetService"] = service;
        }
        return context.Get<BudgetService>("BudgetService");
    }

    private IBudgetSnapshotService GetOrCreateBudgetSnapshotService()
    {
        if (!context.ContainsKey("BudgetSnapshotService"))
        {
            var service = Substitute.For<IBudgetSnapshotService>();
            var snapshotStore = new Dictionary<(int month, int year), BudgetSnapshot>();
            
            // Setup GetSnapshotAsync to return snapshots from our store
            service.GetSnapshotAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(callInfo =>
                {
                    var month = callInfo.ArgAt<int>(0);
                    var year = callInfo.ArgAt<int>(1);
                    snapshotStore.TryGetValue((month, year), out var snapshot);
                    return Task.FromResult(snapshot);
                });
            
            // Setup SaveSnapshotAsync to store snapshots
            service.SaveSnapshotAsync(Arg.Any<BudgetSnapshot>())
                .Returns(callInfo =>
                {
                    var snapshot = callInfo.ArgAt<BudgetSnapshot>(0);
                    snapshotStore[(snapshot.Month, snapshot.Year)] = snapshot;
                    return Task.CompletedTask;
                });
            
            context["BudgetSnapshotService"] = service;
            context["SnapshotStore"] = snapshotStore;
        }
        return context.Get<IBudgetSnapshotService>("BudgetSnapshotService");
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

        service.GetAllFutureAllocationsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var month = callInfo.ArgAt<int>(0);
                var year = callInfo.ArgAt<int>(1);
                var futureAllocations = allocations
                    .Where(a => a.Year > year || (a.Year == year && a.Month > month))
                    .ToList();
                return Task.FromResult((IEnumerable<CategoryAllocation>)futureAllocations);
            });
    }

    private void SetupTransactionMocks(List<TransactionSplit> transactionSplits, ITransactionManagementService service)
    {
        var transactionSplitResponses = transactionSplits.Select(ts =>
            new TransactionSplitResponse(
                Id: ts.Id,
                CategoryAllocationId: ts.CategoryAllocationId,
                TransactionId: 1,
                TransactionDate: ts.Transaction?.TransactionDate ?? DateTime.MinValue,
                CategoryName: ts.CategoryAllocationId.HasValue ? "Some Category" : "Income",
                Amount: ts.Amount,
                Description: ts.Description
            )).ToList();

        service.GetTransactionSplitsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(transactionSplitResponses));

        service.GetTransactionSplitsByMonthAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var date = callInfo.Arg<DateTime>();//TODO Stop this from blowing up.
                var matchingSplits = transactionSplits
                    .Where(ts => ts.Transaction != null &&
                                 ts.Transaction.TransactionDate.Month == date.Month &&
                                 ts.Transaction.TransactionDate.Year == date.Year)
                    .Select(ts => new TransactionSplitResponse(
                        Id: ts.Id,
                        CategoryAllocationId: ts.CategoryAllocationId,
                        TransactionId: 1,
                        TransactionDate: ts.Transaction?.TransactionDate ?? DateTime.MinValue,
                        CategoryName: ts.CategoryAllocationId.HasValue ? "Some Category" : "Income",
                        Amount: ts.Amount,
                        Description: ts.Description
                    ))
                    .ToList();
                return Task.FromResult(matchingSplits);
            });

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
                        TransactionDate: ts.Transaction?.TransactionDate ?? DateTime.MinValue,
                        CategoryName: "Some Category",
                        Amount: ts.Amount,
                        Description: ts.Description
                    ))
                    .ToList();
                return Task.FromResult(matchingSplits);
            });
    }

    private void SetupUserMocks(DateTime accountCreationDate, IUserService service)
    {
        service.GetEarliestActivityDate()
            .Returns(Task.FromResult(accountCreationDate));
    }
}
