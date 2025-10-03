using WNAB.Logic; // LLM-Dev:v4.3 Refactor: store categories on User instead of per-user catKey
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	[Given(@"the following category for user ""(.*)""")]
	public void Giventhefollowingcategoryforuser(string email, DataTable dataTable)
	{
		// Actual
		var user = context.Get<User>("User");
		// Act: add categories directly to the user's collection
		foreach (var row in dataTable.Rows)
		{
			var name = row["CategoryName"].ToString();
			if (user.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;

			var record = CategoryManagementService.CreateCategoryRecord(name!, user.Id == 0 ? 1 : user.Id);
			var category = new Category(record)
			{
				Id = user.Categories.Count + 1 // LLM-Dev v6.5: Assign test ID so CreateTransactionSplitRecord doesn't fail
			};
			user.Categories.Add(category);
		}
		// Store: user already in context; collection updated by reference
	}

	[Given(@"the following categories for user ""(.*)""")]
	public void Giventhefollowingcategoriesforuser(string email, DataTable dataTable)
	{
		// Actual
		var user = context.Get<User>("User");
		// Act: add categories directly to the user's collection
		foreach (var row in dataTable.Rows)
		{
			var name = row["CategoryName"].ToString();
			if (user.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;

			var record = CategoryManagementService.CreateCategoryRecord(name!, user.Id == 0 ? 1 : user.Id);
			var category = new Category(record)
			{
				Id = user.Categories.Count + 1 // LLM-Dev v6.5: Assign test ID so CreateTransactionSplitRecord doesn't fail
			};
			user.Categories.Add(category);
		}
		// Store: user already in context; collection updated by reference
	}

	[When(@"I allocate the following amounts")]
	public void WhenIallocatethefollowingamounts(DataTable dataTable)
	{
		// Actual
		var user = context.Get<User>("User");
		var categories = user.Categories.ToList();
		var allocations = context.ContainsKey("Allocations") ? context.Get<List<CategoryAllocation>>("Allocations") : new List<CategoryAllocation>();

		// Act
		int nextId = allocations.Any() ? allocations.Max(a => a.Id) + 1 : 1;
		foreach (var row in dataTable.Rows)
		{
			var categoryName = row["Category"].ToString();
			var category = categories.Single(c => c.Name == categoryName);
			if (category.Id == 0) category.Id = categories.IndexOf(category) + 1;

			var record = CategoryAllocationManagementService.CreateCategoryAllocationRecord(
				category.Id,
				decimal.Parse(row["BudgetedAmount"].ToString()!),
				int.Parse(row["Month"].ToString()!),
				int.Parse(row["Year"].ToString()!)
			);

			var allocation = new CategoryAllocation(record)
			{
				Id = nextId++,
				Category = category
			};
			allocations.Add(allocation);
		}

		// Store
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
