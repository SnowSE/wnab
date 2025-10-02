using System;
using System.Linq;
using Reqnroll;
using WNAB.Logic; // LLM-Dev:v4.1 Readability pass: Inputs -> Actual -> Act/Store or Assert
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;


public partial class StepDefinitions
{
	[Given(@"the created user")]
	public void Giventhecreateduser(DataTable dataTable)
	{
		// Inputs (expected)
		if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));
		var row = dataTable.Rows.Single();
		var name = dataTable.Header.Contains("Name")
			? row["Name"]
			: $"{row["FirstName"]} {row["LastName"]}";
		var email = row["Email"];
		// Act
		var userRecord = UserManagementService.CreateUserRecord(name, email);
		// Store
		context["UserRecord"] = userRecord;
		WhenICreateTheUser();
	}

	[Given(@"the system has no existing users")]
	public void Giventhesystemhasnoexistingusers(DataTable _)
	{
		// Store
		context["Users"] = new List<User>();
	}

	[Given(@"the following user")]
	public void Giventhefollowinguser(DataTable dataTable)
	{
		// Inputs (expected)
		var row = dataTable.Rows.Single();
		var fullName = $"{row["FirstName"]} {row["LastName"]}";
		// Act
		var userRecord = UserManagementService.CreateUserRecord(fullName, row["Email"]);
		// Store
		context["UserRecord"] = userRecord;
	}

	[When(@"I create the user")]
	public void WhenICreateTheUser()
	{
		// Actual
		var record = context.Get<UserRecord>("UserRecord");
		var users = context.ContainsKey("Users") ? context.Get<List<User>>("Users") : new List<User>();
		// Act
		var user = new User(record);
		// Store
		user.Id = users.Count + 1;
		users.Add(user);
		context["Users"] = users;
		context["User"] = user;
	}

	[Then(@"I should have the following user in the system")]
	public void ThenIShouldHaveTheFollowingUserInTheSystem(DataTable dataTable)
	{
		// Inputs (expected)
		var expectedRow = dataTable.Rows.Single();
		// Actual
		var actual = context.Get<User>("User");
		// Assert
		actual.FirstName.ShouldBe(expectedRow["FirstName"]);
		actual.LastName.ShouldBe(expectedRow["LastName"]);
		if (dataTable.Header.Contains("Email"))
		{
			actual.Email.ShouldBe(expectedRow["Email"]);
		}
		if (dataTable.Header.Contains("IsActive"))
		{
			bool expectedActive = bool.Parse(expectedRow["IsActive"].ToString());
			actual.IsActive.ShouldBe(expectedActive);
		}
	}

}
