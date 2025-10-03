using WNAB.Logic; 
using WNAB.Logic.Data;
using Shouldly;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	// prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
	// Rule: Use the services where possible.
	// Rule: functions may only have datatable as a parameter or no parameter.

	[When(@"I allocate the following amounts")]
	public void WhenIallocatethefollowingamounts(DataTable dataTable)
	{
		// Get user and categories from context
		var user = context.Get<User>("User");
		var categories = user.Categories.ToList();
		
		// Create allocation records first
		var allocationRecords = new List<CategoryAllocationRecord>();
		foreach (var row in dataTable.Rows)
		{
			var categoryName = row["Category"].ToString()!;
			var category = categories.Single(c => c.Name == categoryName);
			
			var record = CategoryAllocationManagementService.CreateCategoryAllocationRecord(
				category.Id,
				decimal.Parse(row["BudgetedAmount"].ToString()!),
				int.Parse(row["Month"].ToString()!),
				int.Parse(row["Year"].ToString()!)
			);
			allocationRecords.Add(record);
		}

		// Act: Convert records to objects
		var allocations = context.ContainsKey("Allocations") ? context.Get<List<CategoryAllocation>>("Allocations") : new List<CategoryAllocation>();
		int nextId = allocations.Any() ? allocations.Max(a => a.Id) + 1 : 1;
		
		foreach (var record in allocationRecords)
		{
			var category = categories.Single(c => c.Id == record.CategoryId);

			var allocation = new CategoryAllocation(record)
			// the only thing that should ever be set here is an ID, nothing else.
			{
				Id = nextId++,
				Category = category // Set the navigation property for test validation
			};
			allocations.Add(allocation);
		}

		// Store: Store converted objects
		context["Allocations"] = allocations;
	}

	[Then(@"I should have the following category allocations")]
	public void ThenIshouldhavethefollowingcategoryallocationsforuser(DataTable dataTable)
	{
		// Inputs (expected)
		var expectedRows = dataTable.Rows.ToList();
		// Actual
		var allocations = context.ContainsKey("Allocations") ? context.Get<List<CategoryAllocation>>("Allocations") : new List<CategoryAllocation>();
		// Assert
		allocations.Count.ShouldBe(expectedRows.Count);

		foreach (var row in expectedRows)
		{
			var categoryName = row["Category"].ToString();
			var month = int.Parse(row["Month"].ToString()!);
			var year = int.Parse(row["Year"].ToString()!);
			var expectedAmount = decimal.Parse(row["BudgetedAmount"].ToString()!);
			var match = allocations.FirstOrDefault(a => a.Category.Name == categoryName && a.Month == month && a.Year == year);
			match.ShouldNotBeNull();
			match!.BudgetedAmount.ShouldBe(expectedAmount);
		}
	}
}
