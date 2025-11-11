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

[Binding]
public class ReadyToAssignWithSnapshotsStepDefinitions
{
    private ICategoryAllocationManagementService _categoryAllocationManagementService;
    private ITransactionManagementService _transactionManagementService;
    private HttpClient _httpClient;
    private BudgetService _budgetService;
    private List<CategoryAllocation> _allocations;
    private List<TransactionSplit> _transactionSplits;
    private BudgetSnapshot? _previousSnapshot;
    private BudgetSnapshot? _resultSnapshot;
    private DateTime _accountCreationDate;
    private decimal _actualRTA;
    private Dictionary<int, List<CategoryAllocation>> _allocationsByMonth;
    private Dictionary<int, List<TransactionSplit>> _transactionsByMonth;

    public ReadyToAssignWithSnapshotsStepDefinitions()
    {
        _categoryAllocationManagementService = Substitute.For<ICategoryAllocationManagementService>();
        _transactionManagementService = Substitute.For<ITransactionManagementService>();
        _httpClient = Substitute.For<HttpClient>();
        _budgetService = new BudgetService(_httpClient, _categoryAllocationManagementService, _transactionManagementService);
        _allocations = new List<CategoryAllocation>();
        _transactionSplits = new List<TransactionSplit>();
        _allocationsByMonth = new Dictionary<int, List<CategoryAllocation>>();
        _transactionsByMonth = new Dictionary<int, List<TransactionSplit>>();
    }

    [Given(@"the account was created on (.*)")]
    public void GivenTheAccountWasCreatedOn(string dateString)
    {
        _accountCreationDate = DateTime.Parse(dateString);
    }

    [Given(@"the following income exists for (.*) (.*)")]
    public void GivenTheFollowingIncomeExistsFor(string monthName, int year, Table table)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var monthKey = year * 100 + month;

        foreach (var row in table.Rows)
        {
            var transactionSplit = new TransactionSplit
            {
                Id = _transactionSplits.Count + 1,
                CategoryAllocationId = null,
                Amount = decimal.Parse(row["Amount"]),
                Description = row["Description"]
            };
            _transactionSplits.Add(transactionSplit);

            if (!_transactionsByMonth.ContainsKey(monthKey))
            {
                _transactionsByMonth[monthKey] = new List<TransactionSplit>();
            }
            _transactionsByMonth[monthKey].Add(transactionSplit);
        }

        SetupTransactionMocks();
    }

    [Given(@"the following category allocations exist for (.*) (.*)")]
    public void GivenTheFollowingCategoryAllocationsExistFor(string monthName, int year, Table table)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var monthKey = year * 100 + month;

        foreach (var row in table.Rows)
        {
            var allocation = new CategoryAllocation
            {
                Id = _allocations.Count + 1,
                CategoryId = int.Parse(row["CategoryId"]),
                BudgetedAmount = decimal.Parse(row["BudgetedAmount"]),
                Month = month,
                Year = year
            };
            _allocations.Add(allocation);

            if (!_allocationsByMonth.ContainsKey(monthKey))
            {
                _allocationsByMonth[monthKey] = new List<CategoryAllocation>();
            }
            _allocationsByMonth[monthKey].Add(allocation);
        }

        SetupAllocationMocks();
    }

    [Given(@"the following spending exists for (.*) (.*)")]
    public void GivenTheFollowingSpendingExistsFor(string monthName, int year, Table table)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        var monthKey = year * 100 + month;

        foreach (var row in table.Rows)
        {
            var categoryId = int.Parse(row["CategoryId"]);
            var allocation = _allocations.FirstOrDefault(a => a.CategoryId == categoryId && a.Month == month && a.Year == year);

            var transactionSplit = new TransactionSplit
            {
                Id = _transactionSplits.Count + 1,
                CategoryAllocationId = allocation?.Id,
                Amount = decimal.Parse(row["Amount"]),
                Description = row["Description"]
            };
            _transactionSplits.Add(transactionSplit);

            if (!_transactionsByMonth.ContainsKey(monthKey))
            {
                _transactionsByMonth[monthKey] = new List<TransactionSplit>();
            }
            _transactionsByMonth[monthKey].Add(transactionSplit);
        }

        SetupTransactionMocks();
    }

    [Given(@"I have a previous snapshot for (.*) (.*) with RTA of (.*)")]
    public void GivenIHaveAPreviousSnapshotWithRTA(string monthName, int year, decimal rta)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        _previousSnapshot = new BudgetSnapshot
        {
            Month = month,
            Year = year,
            RTA = rta,
            Categories = new List<CategorySnapshotData>()
        };
    }

    [Given(@"the previous snapshot has category (.*) with assigned (.*), activity (.*), and available (.*)")]
    public void GivenThePreviousSnapshotHasCategory(int categoryId, decimal assigned, decimal activity, decimal available)
    {
        if (_previousSnapshot == null)
        {
            throw new InvalidOperationException("Previous snapshot must be initialized first");
        }

        _previousSnapshot.Categories.Add(new CategorySnapshotData
        {
            CategoryId = categoryId,
            AssignedValue = assigned,
            Activity = activity,
            Available = available
        });
    }

    [When(@"I rebuild snapshots to (.*) (.*)")]
    public async Task WhenIRebuildSnapshotsTo(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        _resultSnapshot = await _budgetService.RebuildSnapshots(null, month, year, _accountCreationDate);
    }

    [When(@"I build snapshot from (.*) to (.*) (.*)")]
    public async Task WhenIBuildSnapshotFromTo(string fromMonthName, string toMonthName, int year)
    {
        var toMonth = DateTime.Parse($"1 {toMonthName} {year}").Month;
        _resultSnapshot = await _budgetService.RebuildSnapshots(_previousSnapshot, toMonth, year, _accountCreationDate);
    }

    [When(@"I calculate RTA for (.*) (.*) with the snapshot")]
    public async Task WhenICalculateRTAWithTheSnapshot(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        _actualRTA = await _budgetService.CalculateReadyToAssign(month, year, _previousSnapshot, _accountCreationDate);
    }

    [When(@"I calculate RTA for (.*) (.*) without a snapshot")]
    public async Task WhenICalculateRTAWithoutSnapshot(string monthName, int year)
    {
        var month = DateTime.Parse($"1 {monthName} {year}").Month;
        _actualRTA = await _budgetService.CalculateReadyToAssign(month, year, null, _accountCreationDate);
    }

    [Then(@"the snapshot for (.*) (.*) should have RTA of (.*)")]
    public void ThenTheSnapshotShouldHaveRTA(string monthName, int year, decimal expectedRTA)
    {
        _resultSnapshot.ShouldNotBeNull();
        _resultSnapshot.RTA.ShouldBe(expectedRTA);
    }

    [Then(@"the snapshot should have category (.*) with assigned (.*), activity (.*), and available (.*)")]
    public void ThenTheSnapshotShouldHaveCategory(int categoryId, decimal assigned, decimal activity, decimal available)
    {
        _resultSnapshot.ShouldNotBeNull();
        var category = _resultSnapshot.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        category.ShouldNotBeNull();
        category.AssignedValue.ShouldBe(assigned);
        category.Activity.ShouldBe(activity);
        category.Available.ShouldBe(available);
    }

    [Then(@"the RTA should be (.*)")]
    public void ThenTheRTAShouldBe(decimal expectedRTA)
    {
        _actualRTA.ShouldBe(expectedRTA);
    }

    private void SetupAllocationMocks()
    {
        _categoryAllocationManagementService.GetAllAllocationsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_allocations));

        _categoryAllocationManagementService.GetAllocationsForCategoryAsync(Arg.Any<int>())
            .Returns(callInfo =>
            {
                var categoryId = callInfo.Arg<int>();
                var categoryAllocations = _allocations.Where(a => a.CategoryId == categoryId).ToList();
                return Task.FromResult(categoryAllocations);
            });
    }

    private void SetupTransactionMocks()
    {
        var transactionSplitResponses = _transactionSplits.Select(ts =>
            new TransactionSplitResponse(
                Id: ts.Id,
                CategoryAllocationId: ts.CategoryAllocationId,
                TransactionId: 1,
                CategoryName: ts.CategoryAllocationId.HasValue ? "Some Category" : "Income",
                Amount: ts.Amount,
                Description: ts.Description
            )).ToList();

        _transactionManagementService.GetTransactionSplitsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(transactionSplitResponses));

        _transactionManagementService.GetTransactionSplitsForAllocationAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var allocationId = callInfo.Arg<int>();
                var matchingSplits = _transactionSplits
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
