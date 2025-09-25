using Reqnroll;

namespace WNAB.Tests.Unit.StepDefinitions;

[Binding]
public class CategoryEntryStepDefinitions
{
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
}