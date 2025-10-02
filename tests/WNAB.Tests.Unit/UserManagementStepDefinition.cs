using System;
using System.Linq;
using Reqnroll;
using WNAB.Logic; // LLM-Dev: Use services to create DTO records
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{
	[Given(@"the created user")]
	public void Giventhecreateduser(DataTable dataTable)
	{
		// LLM-Dev:v3 Gherkin-callable hybrid step to create a user directly from the table.
		if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));
		var row = dataTable.Rows.Single();
		// Support either a single "Name" column or separate FirstName/LastName columns.
		var name = dataTable.Header.Contains("Name")
			? row["Name"]
			: $"{row["FirstName"]} {row["LastName"]}";
		var email = row["Email"];
		// Stage a DTO and delegate to existing creation logic for consistency.
		var userRecord = UserManagementService.CreateUserRecord(name, email);
		context["UserRecord"] = userRecord;
		WhenICreateTheUser();
	}

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers(DataTable _)
	{
		// LLM-Dev:v2 Initialize empty user list for scenario isolation
		context["Users"] = new List<User>();
	}

	[Given(@"the following user")]
	public void Giventhefollowinguser(DataTable dataTable)
	{
		// LLM-Dev:v2 Build a DTO using the service and stage only the record in context.
		var row = dataTable.Rows.Single();
		var fullName = $"{row["FirstName"]} {row["LastName"]}";
		// LLM-Dev:v2 Build record via static method; no HttpClient or service instance needed
		var userRecord = UserManagementService.CreateUserRecord(fullName, row["Email"]);
		context["UserRecord"] = userRecord; // LLM-Dev:v2 Store DTO; entity will be created during When
	}

	[When(@"I create the user")]
	public void WhenICreateTheUser()
	{
		// LLM-Dev:v3 Use entity convenience ctor from staged UserRecord
		var record = context.Get<UserRecord>("UserRecord");
		var users = context.ContainsKey("Users") ? context.Get<List<User>>("Users") : new List<User>();
		var user = new User(record);
		// Assign a simple incremental Id
		user.Id = users.Count + 1;
		users.Add(user);
		context["Users"] = users;
		context["User"] = user; // LLM-Dev:v2 Expose created entity for subsequent steps
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIShouldHaveTheFollowingUserInTheSystem(DataTable dataTable)
	{
		var expectedRow = dataTable.Rows.Single();
		var users = context.Get<List<User>>("Users");
		var email = expectedRow["Email"];
		var actual = users.SingleOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
		actual.ShouldNotBeNull(); // LLM-Dev: Existence check
		actual!.FirstName.ShouldBe(expectedRow["FirstName"]);
		actual.LastName.ShouldBe(expectedRow["LastName"]);
		// If IsActive column provided, assert; else skip (defensive for future scenarios)
		if (dataTable.Header.Contains("IsActive"))
		{
			bool expectedActive = bool.Parse(expectedRow["IsActive"].ToString());
			actual.IsActive.ShouldBe(expectedActive);
		}
	}

}
