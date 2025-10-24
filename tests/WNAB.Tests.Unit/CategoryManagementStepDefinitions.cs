using WNAB.Data;
using WNAB.Services;
using Shouldly;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
    // prescribed pattern: (Given) creates and stores records, (When) uses services to create objects, (Then) compares objects
    // Rule: Use the services where possible.
    // Rule: functions may only have datatable as a parameter or no parameter.

    [Given(@"the following category")]
    public void GivenTheFollowingCategory(DataTable dataTable)
    {
        // Inputs: parse category data and create records
        var user = context.Get<User>("User");
        var categoryRecords = context.ContainsKey("CategoryRecords") 
            ? context.Get<List<CategoryRecord>>("CategoryRecords") 
            : new List<CategoryRecord>();
        
        foreach (var row in dataTable.Rows)
        {
            var name = row["CategoryName"].ToString()!;
            // Act: Create category record
            var record = new CategoryRecord(name);
            categoryRecords.Add(record);
        }
        
        // Store: Store records for later conversion
        context["CategoryRecords"] = categoryRecords;
    }

    [Given(@"the following categories")]
    public void GivenTheFollowingCategories(DataTable dataTable)
    {
        // Inputs: parse category data and create records  
        var user = context.Get<User>("User");
        var categoryRecords = context.ContainsKey("CategoryRecords") 
            ? context.Get<List<CategoryRecord>>("CategoryRecords") 
            : new List<CategoryRecord>();
        
        foreach (var row in dataTable.Rows)
        {
            var name = row["CategoryName"].ToString()!;
            // Act: Create category record
            var record = new CategoryRecord(name);
            categoryRecords.Add(record);
        }
        
        // Store: Store records for later conversion
        context["CategoryRecords"] = categoryRecords;
    }

    [Given(@"the created category for user ""(.*)""")]
    public void GivenTheCreatedCategoryForUserWithEmail(string email, DataTable dataTable)
    {
        // Create categories immediately (combines Given + When steps)
        GivenTheFollowingCategory(dataTable);
        WhenICreateTheCategory();
    }

    [Given(@"the created categories for user ""(.*)""")]
    public void GivenTheCreatedCategoriesForUserWithEmail(string email, DataTable dataTable)
    {
        // Create categories immediately (combines Given + When steps)
        GivenTheFollowingCategories(dataTable);
        WhenICreateTheCategories();
    }

    [Given(@"the existing categories")]
    public void GivenTheExistingCategories(DataTable dataTable)
    {
        // Inputs: parse category data and create categories directly
        var user = context.Get<User>("User");
        
        // Initialize user categories if not already done
        if (user.Categories == null)
            user.Categories = new List<Category>();

        var existingCategories = user.Categories.ToList();
        int nextCategoryId = existingCategories.Any() ? existingCategories.Max(c => c.Id) + 1 : 1;
        
        foreach (var row in dataTable.Rows)
        {
            var name = row["CategoryName"].ToString()!;
            // Act: Create category record
            var record = new CategoryRecord(name);
            
            // Convert to category object immediately
            var category = new Category
            {
                Id = nextCategoryId++,
                Name = name,
                UserId = user.Id == 0 ? 1 : user.Id,
                User = user
            };
            
            user.Categories.Add(category);
        }
    }

    [When(@"I create the category")]
    public void WhenICreateTheCategory()
    {
        // Actual: Convert category records to objects
        var user = context.Get<User>("User");
        var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
        var convertedCategories = new List<Category>();
        int categoryId = 1;
        
        foreach (var record in categoryRecords)
        {
            var category = new Category
            {
                Id = categoryId++,
                Name = record.Name,
                UserId = user.Id,
                User = user
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

    [When(@"I create the categories")]
    public void WhenICreateTheCategories()
    {
        // Actual: Convert category records to objects
        var user = context.Get<User>("User");
        var categoryRecords = context.Get<List<CategoryRecord>>("CategoryRecords");
        var convertedCategories = new List<Category>();
        int categoryId = user.Categories?.Any() == true ? user.Categories.Max(c => c.Id) + 1 : 1;
        
        foreach (var record in categoryRecords)
        {
            var category = new Category
            {
                Id = categoryId++,
                Name = record.Name,
                UserId = user.Id,
                User = user
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

    [Then(@"I should have the following category in the system")]
    public void ThenIShouldHaveTheFollowingCategoryInTheSystem(DataTable dataTable)
    {
        // Inputs (expected)
        var expectedRow = dataTable.Rows.Single();
        var expectedCategoryName = expectedRow["CategoryName"].ToString();
        
        // Actual
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        
        // Assert
        categories.Count.ShouldBe(1);
        var actualCategory = categories.Single();
        actualCategory.Name.ShouldBe(expectedCategoryName);
    }

    [Then(@"I should have the following categories in the system")]
    public void ThenIShouldHaveTheFollowingCategoriesInTheSystem(DataTable dataTable)
    {
        // Inputs (expected)
        var expectedRows = dataTable.Rows.ToList();
        var expectedCategoryNames = expectedRows.Select(row => row["CategoryName"].ToString()).ToList();
        
        // Actual
        var user = context.Get<User>("User");
        var categories = user.Categories.ToList();
        
        // Assert
        categories.Count.ShouldBe(expectedCategoryNames.Count);
        
        foreach (var expectedName in expectedCategoryNames)
        {
            var matchingCategory = categories.FirstOrDefault(c => c.Name == expectedName);
            matchingCategory.ShouldNotBeNull($"Category '{expectedName}' should exist in the system");
        }
    }
}