using System;
using Reqnroll;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	[Given(@"the following category for user ""(.*)""")]
	public void Giventhefollowingcategoryforuser(DataTable dataTable)
	{
		context.Pending();
	}

	[When(@"I allocate the following amounts")]
	public void WhenIallocatethefollowingamounts(DataTable dataTable)
	{
		context.Pending();
	}

	[Then(@"I should have the following category allocations for user ""(.*)""")]
	public void ThenIshouldhavethefollowingcategoryallocationsforuser(DataTable dataTable)
	{
		context.Pending();
	}


	[Given(@"the following categories for user ""(.*)""")]
	public void Giventhefollowingcategoriesforuser(DataTable dataTable)
	{
		context.Pending();
	}

}
