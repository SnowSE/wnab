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

	[Given(@"the following category")]
	public void Giventhefollowingcategoryforuser(DataTable dataTable)
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

	[Given(@"the following categories")]
	public void Giventhefollowingcategoriesforuser(DataTable dataTable)
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

	[Given(@"I create the category")]
	public void GivenICreateTheCategory()
	{
		// Actual: Convert category records to objects
		var user = context.Get<User>("User");
		var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
		var convertedCategories = new List<Category>();
		int categoryId = 1;
		
		foreach (var record in categoryRecords)
		{
			var category = new Category(record)
			{
				Id = categoryId++
			};
			convertedCategories.Add(category);
		}
		
		// Initialize user categories if not already done
		if (user.Categories == null)
			user.Categories = new List<Category>();
		
		foreach (var category in convertedCategories)
		{
			user.Categories.Add(category);
		}
	}

	[Given(@"I create the categories")]
	public void GivenICreateTheCategories()
	{
		// Actual: Convert category records to objects
		var user = context.Get<User>("User");
		var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
		var convertedCategories = new List<Category>();
		int categoryId = 1;
		
		foreach (var record in categoryRecords)
		{
			var category = new Category(record)
			// the only thing that should ever be set here is an ID, nothing else.
			{
				Id = categoryId++
			};
			convertedCategories.Add(category);
		}
		
		// Initialize user categories if not already done
		if (user.Categories == null)
			user.Categories = new List<Category>();
		
		foreach (var category in convertedCategories)
		{
			user.Categories.Add(category);
		}
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
				// the only thing that should ever be set here is an ID, nothing else.
				{
					Id = categoryId++
				};
				convertedCategories.Add(category);
			}
			
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
			// the only thing that should ever be set here is an ID, nothing else.
			{
				Id = nextId++
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
