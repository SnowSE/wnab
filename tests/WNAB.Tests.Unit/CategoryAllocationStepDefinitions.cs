using WNAB.Logic; // LLM-Dev: Use services to create DTO records
using WNAB.Logic.Data;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	[Given(@"the following category for user ""(.*)""")]
	public void Giventhefollowingcategoryforuser(string email, DataTable dataTable)
	{
		AddCategoriesForUser(email, dataTable);
	}

	[Given(@"the following categories for user ""(.*)""")]
	public void Giventhefollowingcategoriesforuser(string email, DataTable dataTable)
	{
		AddCategoriesForUser(email, dataTable);
	}

	[When(@"I allocate the following amounts")]
	public void WhenIallocatethefollowingamounts(DataTable dataTable)
	{
		// LLM-Dev: Build CategoryAllocationRecord before mapping to in-memory entity; no HTTP calls.
		var user = context.Get<User>("User");
		var catKey = $"Categories:{user.Email.ToLower()}";
		var allocKey = $"Allocations:{user.Email.ToLower()}";
		var categories = context.ContainsKey(catKey) ? context.Get<List<Category>>(catKey) : new List<Category>();
		var allocations = context.ContainsKey(allocKey) ? context.Get<List<CategoryAllocation>>(allocKey) : new List<CategoryAllocation>();
		var stagedRecords = new List<CategoryAllocationRecord>();
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
			stagedRecords.Add(record);
			allocations.Add(new CategoryAllocation
			{
				Id = nextId++,
				CategoryId = record.CategoryId,
				Category = category,
				Month = record.Month,
				Year = record.Year,
				BudgetedAmount = record.BudgetedAmount,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			});
		}
		context[$"CategoryAllocationRecords:{user.Email.ToLower()}"] = stagedRecords;
		context[allocKey] = allocations;
	}

	[Then(@"I should have the following category allocations for user ""(.*)""")]
	public void ThenIshouldhavethefollowingcategoryallocationsforuser(string email, DataTable dataTable)
	{
		var allocKey = $"Allocations:{email.ToLower()}";
		var allocations = context.ContainsKey(allocKey) ? context.Get<List<CategoryAllocation>>(allocKey) : new List<CategoryAllocation>();
		allocations.Count.ShouldBe(dataTable.Rows.Count());

		foreach (var row in dataTable.Rows)
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

	// LLM-Dev: Helpers centralize per-user collections in ScenarioContext; create CategoryRecord with service.
    private void AddCategoriesForUser(string email, DataTable dataTable)
    {
		var catKey = $"Categories:{email.ToLower()}";
		var categories = context.ContainsKey(catKey) ? context.Get<List<Category>>(catKey) : new List<Category>();
		foreach (var row in dataTable.Rows)
		{
			var name = row["CategoryName"].ToString();
			if (categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
			// Build DTO via service
			var stagedUser = context.Get<User>("User");
			var record = CategoryManagementService.CreateCategoryRecord(name!, stagedUser.Id == 0 ? 1 : stagedUser.Id);
			var recordKey = $"CategoryRecords:{email.ToLower()}";
			var stagedRecords = context.ContainsKey(recordKey) ? context.Get<List<CategoryRecord>>(recordKey) : new List<CategoryRecord>();
			stagedRecords.Add(record);
			context[recordKey] = stagedRecords;

			// Mirror in-memory entity for assertions
			categories.Add(new Category
			{
				Name = name!,
				UserId = stagedUser.Id == 0 ? 1 : stagedUser.Id,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			});
		}
		context[catKey] = categories;
    }
}
