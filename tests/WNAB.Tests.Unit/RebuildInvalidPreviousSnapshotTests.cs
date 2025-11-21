using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using WNAB.MVM;
using WNAB.Data;
using WNAB.SharedDTOs;
using Xunit;

namespace WNAB.Tests.Unit
{
    public class RebuildInvalidPreviousSnapshotTests
    {
        [Fact]
        public async Task RebuildsInvalidPreviousSnapshot()
        {
            // Arrange: target = Nov 2025, previous = Oct 2025
            var prevMonth = 10; var prevYear = 2025;
            var targetMonth = 11; var targetYear = 2025;

            // Mock user service: earliest activity is Oct 1, 2025
            var userService = Substitute.For<IUserService>();
            userService.GetEarliestActivityDate().Returns(Task.FromResult(new DateTime(2025, 10, 1)));

            // Mock category allocation service: allocations for Oct/Nov
            var allocations = new List<CategoryAllocation>
            {
                new CategoryAllocation { Id = 1, CategoryId = 1, BudgetedAmount = 100m, Month = 11, Year = 2025 }
            };
            var allocationService = Substitute.For<ICategoryAllocationManagementService>();
            allocationService.GetAllAllocationsAsync().Returns(Task.FromResult(allocations));
            allocationService.GetAllocationsForCategoryAsync(Arg.Any<int>())
                .Returns(callInfo =>
                {
                    var categoryId = callInfo.Arg<int>();
                    var list = allocations.Where(a => a.CategoryId == categoryId).ToList();
                    return Task.FromResult(list);
                });
            allocationService.GetAllFutureAllocationsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult((IEnumerable<CategoryAllocation>)new List<CategoryAllocation>()));

            // Mock transaction service: income in Oct and Nov, and spending in Nov for allocation 1
            var transactionService = Substitute.For<ITransactionManagementService>();

            // Oct income: 300 (for base snapshot creation path)
            var octIncome = new List<TransactionSplitResponse>
            {
                new TransactionSplitResponse(1, null, 1, new DateTime(2025,10,5), "Income", 300m, "Initial")
            };
            // Nov income: 300
            var novIncome = new List<TransactionSplitResponse>
            {
                new TransactionSplitResponse(2, null, 2, new DateTime(2025,11,1), "Income", 300m, "Monthly")
            };

            transactionService.GetTransactionSplitsByMonthAsync(Arg.Is<DateTime>(d => d.Month==10 && d.Year==2025), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(octIncome));
            transactionService.GetTransactionSplitsByMonthAsync(Arg.Is<DateTime>(d => d.Month==11 && d.Year==2025), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(novIncome));

            // For allocation splits: Nov spending 50 on allocation id 1
            var allocationSplits = new List<TransactionSplit>
            {
                new TransactionSplit { Id = 1, CategoryAllocationId = 1, Amount = 50m }
            };
            var allocationResponses = allocationSplits.Select(ts =>
                    new TransactionSplitResponse(ts.Id, ts.CategoryAllocationId, 1, DateTime.MinValue, "Some Category", ts.Amount, "")).ToList();
            transactionService.GetTransactionSplitsForAllocationAsync(1, Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult(allocationResponses));

            // Mock budget snapshot service with a store: previous snapshot exists but is invalid
            var snapshotStore = new Dictionary<(int month, int year), BudgetSnapshot>();
            var budgetSnapshotService = Substitute.For<IBudgetSnapshotService>();

            // Put an invalid previous snapshot in store
            snapshotStore[(prevMonth, prevYear)] = new BudgetSnapshot
            {
                Month = prevMonth,
                Year = prevYear,
                SnapshotReadyToAssign = 100m,
                Categories = new List<CategorySnapshotData>(),
                IsValid = false
            };

            budgetSnapshotService.GetSnapshotAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(callInfo =>
                {
                    var m = callInfo.ArgAt<int>(0);
                    var y = callInfo.ArgAt<int>(1);
                    snapshotStore.TryGetValue((m, y), out var s);
                    return Task.FromResult(s);
                });

            budgetSnapshotService.SaveSnapshotAsync(Arg.Any<BudgetSnapshot>())
                .Returns(callInfo =>
                {
                    var s = callInfo.ArgAt<BudgetSnapshot>(0);
                    // mark saved snapshots as valid (simulate DB service behavior)
                    s.IsValid = true;
                    snapshotStore[(s.Month, s.Year)] = s;
                    return Task.CompletedTask;
                });

            // Construct service under test
            var budgetService = new BudgetService(allocationService, transactionService, userService, budgetSnapshotService);

            // Act: rebuild Nov 2025
            var result = await budgetService.RebuildSnapshots(targetMonth, targetYear);

            // Assert: previous snapshot should have been rebuilt and marked valid in the store
            snapshotStore.ShouldContainKey((prevMonth, prevYear));
            var rebuiltPrev = snapshotStore[(prevMonth, prevYear)];
            rebuiltPrev.IsValid.ShouldBeTrue();

            // And target snapshot exists and has expected RTA (previous RTA + nov income - allocations - overspend)
            snapshotStore.ShouldContainKey((targetMonth, targetYear));
            var target = snapshotStore[(targetMonth, targetYear)];
            // For our inputs: previous snapshot (after rebuild) will have SnapshotReadyToAssign computed from oct income/allocation logic; we check that target exists and is valid
            target.IsValid.ShouldBeTrue();
            target.SnapshotReadyToAssign.ShouldBeGreaterThan(0);
        }
    }
}
