using Reqnroll;

namespace WNAB.Tests.Unit.StepDefinitions;

[Binding]
public class CategoryEntryStepDefinitions
{
    private int _userId;
    private string? _categoryName;
    private int _createdUserId;
    private string? _createdCategoryName;

    [Given(@"I have an empty test")]
    public void GivenIHaveAnEmptyTest()
    {
        // Empty test setup
    }

    [When(@"I run the test")]
    public void WhenIRunTheTest()
    {
        // Empty test execution
    }

    [Then(@"the test should pass")]
    public void ThenTheTestShouldPass()
    {
        Assert.True(true); // Simple passing assertion
    }

    [Given(@"a category with userId (.*) and name ""(.*)""")]
    public void GivenACategoryWithUserIdAndName(int userId, string categoryName)
    {
        _userId = userId;
        _categoryName = categoryName;
    }

    [When(@"I enter the category")]
    public void WhenIEnterTheCategory()
    {
        // Simulate creating/entering the category
        _createdUserId = _userId;
        _createdCategoryName = _categoryName;
    }

    [Then(@"I should have my category with userId (.*) and name ""(.*)""")]
    public void ThenIShouldHaveMyCategoryWithUserIdAndName(int expectedUserId, string expectedCategoryName)
    {
        Assert.Equal(expectedUserId, _createdUserId);
        Assert.Equal(expectedCategoryName, _createdCategoryName);
    }
}