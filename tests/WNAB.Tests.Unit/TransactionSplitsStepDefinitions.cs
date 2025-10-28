using WNAB.Data;
using WNAB.SharedDTOs;
using Shouldly;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
    // prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
    // Rule: Use the services where possible.
    // Rule: functions may only have datatable as a parameter or no parameter.

    [Given(@"the following transaction splits")]
    public void GivenTheFollowingTransactionSplits(DataTable dataTable)
    {
        // Get user and categories from context
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        var transactions = context.Get<List<Transaction>>("Transactions");
        var transaction = transactions.First(); // Use first available transaction
        
        // Create TransactionSplitRecord objects from the table using the service method
        var splitRecords = new List<TransactionSplitRecord>();
        foreach (var row in dataTable.Rows)
        {
            var categoryName = row["Category"].ToString()!;
            var amount = decimal.Parse(row["Amount"].ToString()!);
            var category = categories.Single(c => c.Name == categoryName);
            
            // Create split record
            var splitRecord = new TransactionSplitRecord(
                category.Id,
                transaction.Id,
                amount,
                false,
                null
            );
            splitRecords.Add(splitRecord);
        }
        
        // Store the split records
        context["TransactionSplitRecords"] = splitRecords;
    }

    [When(@"I create the transaction splits")]
    public void WhenICreateTheTransactionSplits()
    {
        // Actual: Get records from context
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        var transactions = context.Get<List<Transaction>>("Transactions");
        var transaction = transactions.First();
        var splitRecords = context.Get<List<TransactionSplitRecord>>("TransactionSplitRecords");
        
        // Act: Convert records to objects
        var transactionSplits = new List<TransactionSplit>();
        int nextSplitId = 1;
        
		foreach (var splitRecord in splitRecords)
		{
			var category = categories.Single(c => c.Id == splitRecord.CategoryAllocationId);
			var split = new TransactionSplit
			{
				Id = nextSplitId++,
				CategoryAllocationId = splitRecord.CategoryAllocationId,
				TransactionId = splitRecord.TransactionId,
				Amount = splitRecord.Amount,
				IsIncome = splitRecord.IsIncome,
				Notes = splitRecord.Notes,
				CategoryAllocation = new CategoryAllocation { Id = splitRecord.CategoryAllocationId, CategoryId = category.Id, Category = category },
				Transaction = transaction
			};
			transactionSplits.Add(split);
		}
		
        // Associate splits with transaction
        transaction.TransactionSplits = transactionSplits;
        
        // Store: Store converted objects
        context["TransactionSplits"] = transactionSplits;
    }

    [Then(@"I should have the following transaction splits")]
    public void ThenIShouldHaveTheFollowingTransactionSplitsFromSplitObjects(DataTable dataTable)
    {
        // Inputs (expected)
        var expectedSplits = dataTable.Rows.Select(row => new
        {
            Category = row["Category"].ToString()!,
            Amount = decimal.Parse(row["Amount"].ToString()!)
        }).ToList();

        // Actual - get splits from converted objects (for tests that use "I create the transaction splits")
        if (context.ContainsKey("TransactionSplits"))
        {
            var actualSplits = context.Get<List<TransactionSplit>>("TransactionSplits");
            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();

            // Assert
            actualSplits.Count.ShouldBe(expectedSplits.Count);
            
            foreach (var expectedSplit in expectedSplits)
            {
                var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);
                // Find matching category allocation
                var allocations = context.ContainsKey("Allocations") ? context.Get<List<CategoryAllocation>>("Allocations") : new List<CategoryAllocation>();
                var expectedAllocation = allocations.FirstOrDefault(a => a.CategoryId == expectedCategory.Id);
                
                var actualSplit = actualSplits.FirstOrDefault(s => 
                    s.CategoryAllocationId == (expectedAllocation?.Id ?? expectedCategory.Id) && s.Amount == expectedSplit.Amount);
                
                actualSplit.ShouldNotBeNull($"Split for category '{expectedSplit.Category}' with amount {expectedSplit.Amount} should exist");
                actualSplit!.Amount.ShouldBe(expectedSplit.Amount);
            }
        }
        // Otherwise fallback to TransactionRecord (for tests that use "I enter the transaction with split")
        else if (context.ContainsKey("TransactionRecord"))
        {
            var actualRecord = context.Get<TransactionRecord>("TransactionRecord");
            var user = context.Get<User>("User");
            var categories = user.Categories.ToList();
            var allocations = context.Get<List<CategoryAllocation>>("Allocations");

            // Assert
            actualRecord.Splits.Count.ShouldBe(expectedSplits.Count);
            for (int i = 0; i < expectedSplits.Count; i++)
            {
                var expectedSplit = expectedSplits[i];
                var actualSplit = actualRecord.Splits[i];
                var expectedCategory = categories.Single(c => c.Name == expectedSplit.Category);
                var expectedAllocation = allocations.Single(a => a.CategoryId == expectedCategory.Id);

                actualSplit.Amount.ShouldBe(expectedSplit.Amount);
                actualSplit.CategoryAllocationId.ShouldBe(expectedAllocation.Id);
            }
        }
        else
        {
            throw new InvalidOperationException("Neither 'TransactionSplits' nor 'TransactionRecord' was found in context");
        }
    }
}