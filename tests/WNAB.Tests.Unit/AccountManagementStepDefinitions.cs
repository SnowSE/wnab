using System;
using Reqnroll;
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{

	[Given(@"the following account for user ""(.*)""")]
	public void Giventhefollowingaccountforuser(string email, DataTable dataTable)
	{
		// LLM-Dev: Parse single account row for specified user email (user must already be staged in context)
		var row = dataTable.Rows.Single();
		var account = new Account
		{
			AccountName = row["AccountName"],
			AccountType = row["AccountType"],
			CachedBalance = decimal.Parse(row["OpeningBalance"]),
			CachedBalanceDate = DateTime.UtcNow,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		// Store a pending accounts list keyed by email
		var key = $"Accounts:{email.ToLower()}";
		var accounts = context.ContainsKey(key) ? context.Get<List<Account>>(key) : new List<Account>();
		accounts.Add(account);
		context[key] = accounts;
	}

	[When(@"I create the user and related accounts")]
	public void WhenIcreatetheuserandrelatedaccounts()
	{
		// LLM-Dev: Assign user Id and link accounts (simple in-memory association)
		var user = context.Get<User>("User");
		var accountsKey = $"Accounts:{user.Email.ToLower()}";
		var accounts = context.ContainsKey(accountsKey) ? context.Get<List<Account>>(accountsKey) : new List<Account>();
		int nextAccountId = 1;
		foreach (var acct in accounts)
		{
			acct.Id = nextAccountId++;
			acct.UserId = user.Id == 0 ? 1 : user.Id; // ensure user has Id
			acct.User = user;
		}
		user.Accounts = accounts;
		if (user.Id == 0) user.Id = 1; // ensure user persists with Id
		// Ensure user list contains this user
		var users = context.ContainsKey("Users") ? context.Get<List<User>>("Users") : new List<User>();
		if (!users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
		{
			users.Add(user);
			context["Users"] = users;
		}
	}

	[Then(@"the user ""(.*)"" should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(string email, DataTable dataTable)
	{
		var users = context.Get<List<User>>("Users");
		var user = users.SingleOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
		user.ShouldNotBeNull();
		var expectedRows = dataTable.Rows.ToList();
		user!.Accounts.Count.ShouldBe(expectedRows.Count);
		for (int i = 0; i < expectedRows.Count; i++)
		{
			var row = expectedRows[i];
			var match = user.Accounts.FirstOrDefault(a => a.AccountName == row["AccountName"]);
			match.ShouldNotBeNull();
			match!.AccountType.ShouldBe(row["AccountType"]);
			if (dataTable.Header.Contains("CachedBalance"))
			{
				match.CachedBalance.ShouldBe(decimal.Parse(row["CachedBalance"]));
			}
		}
	}

}
