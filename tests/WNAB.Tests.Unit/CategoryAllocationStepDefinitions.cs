using WNAB.Logic; 
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	// prescribed pattern: (Given) stores records, (When) converts to objects, (Then) compares objects
	[Given(@"the following category for user ""(.*)""")]
	public void Giventhefollowingcategoryforuser(string email, DataTable dataTable)
	{
		// Inputs: parse category data and create records
		var user = context.Get<User>("User");
		var categoryRecords = context.ContainsKey("CategoryRecords") 
			? context.Get<List<CategoryRecord>>("CategoryRecords") 
			: new List<CategoryRecord>();
		
		foreach (var row in dataTable.Rows)
		{
			var name = row["CategoryName"].ToString()!;
			// Act: Create category record using service
			var record = CategoryManagementService.CreateCategoryRecord(name, user.Id == 0 ? 1 : user.Id);
			categoryRecords.Add(record);
		}
		
		// Store: Store records for later conversion
		context["CategoryRecords"] = categoryRecords;
	}

	[Given(@"the following categories for user ""(.*)""")]
	public void Giventhefollowingcategoriesforuser(string email, DataTable dataTable)
	{
		// Inputs: parse category data and create records  
		var user = context.Get<User>("User");
		var categoryRecords = context.ContainsKey("CategoryRecords") 
			? context.Get<List<CategoryRecord>>("CategoryRecords") 
			: new List<CategoryRecord>();
		
		foreach (var row in dataTable.Rows)
		{
			var name = row["CategoryName"].ToString()!;
			// Act: Create category record using service
			var record = CategoryManagementService.CreateCategoryRecord(name, user.Id == 0 ? 1 : user.Id);
			categoryRecords.Add(record);
		}
		
		// Store: Store records for later conversion
		context["CategoryRecords"] = categoryRecords;
	}

	[When(@"I allocate the following amounts")]
	public void WhenIallocatethefollowingamounts(DataTable dataTable)
	{
		// Actual: First convert category records to objects if not already done
		var user = context.Get<User>("User");
		if (context.ContainsKey("CategoryRecords"))
		{
			var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
			var convertedCategories = new List<Category>();
			int categoryId = 1;
			
			foreach (var record in categoryRecords)
			{
				var category = new Category(record)
				{
					Id = categoryId++,
					User = user
				};
				convertedCategories.Add(category);
			}
			
			user.Categories = convertedCategories;
			context.Remove("CategoryRecords"); // Remove records after conversion
		}
		
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
			{
				Id = nextId++,
				Category = category
			};
			allocations.Add(allocation);
		}

		// Store: Store converted objects
		context["Allocations"] = allocations;
	}

	[Then(@"I should have the following category allocations for user ""(.*)""")]
	public void ThenIshouldhavethefollowingcategoryallocationsforuser(string email, DataTable dataTable)
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
