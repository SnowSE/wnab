using System;
using System.Linq;
using Reqnroll;
using WNAB.Logic; // LLM-Dev:v4.1 Readability pass: Inputs -> Actual -> Assert/Store
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	[Given(@"the following account for user ""(.*)""")]
	public void Giventhefollowingaccountforuser(string email, DataTable dataTable)
	{
		// Inputs: stage minimal row data
		var row = dataTable.Rows.Single();
		var rowsKey = $"StagedAccountRows:{email.ToLower()}";
		var stagedRows = context.ContainsKey(rowsKey)
			? context.Get<List<Dictionary<string, string>>>(rowsKey)
			: new List<Dictionary<string, string>>();
		stagedRows.Add(new Dictionary<string, string>
		{
			["AccountName"] = row["AccountName"],
			["AccountType"] = row["AccountType"],
			["OpeningBalance"] = row["OpeningBalance"],
		});
		// Store
		context[rowsKey] = stagedRows;
	}

	// Alias for readability in features
	[Given(@"I create the accounts")]
	[When(@"I create the accounts")]
	public void WhenICreateTheAccounts()
	{
		// Actual: get staged rows and current user
		if (!context.ContainsKey("User"))
		{
			WhenICreateTheUser();
		}
		var user = context.Get<User>("User");
		var emailKey = user.Email.ToLower();
		var rowsKey = $"StagedAccountRows:{emailKey}";
		var stagedRows = context.ContainsKey(rowsKey)
			? context.Get<List<Dictionary<string, string>>>(rowsKey)
			: new List<Dictionary<string, string>>();

		// Act: create accounts
		var accounts = context.ContainsKey("Accounts") ? context.Get<List<Account>>("Accounts") : new List<Account>();
		int nextAccountId = accounts.Any() ? accounts.Max(a => a.Id) + 1 : 1;

		for (int i = 0; i < stagedRows.Count; i++)
		{
			var r = stagedRows[i];
			var rec = AccountManagementService.CreateAccountRecord(r["AccountName"]);
			var acct = new Account(rec)
			{
				Id = nextAccountId++,
				AccountType = r["AccountType"],
				CachedBalance = decimal.Parse(r["OpeningBalance"]),
				CachedBalanceDate = DateTime.UtcNow,
				UserId = user.Id,
				User = user
			};
			accounts.Add(acct);
		}
		// Store
		context["Accounts"] = accounts;
		user.Accounts = accounts;
	}

	[Then(@"the user ""(.*)"" should have the following accounts")]
	public void Thentheusershouldhavethefollowingaccounts(string email, DataTable dataTable)
	{
		// Inputs (expected)
		var expectedRows = dataTable.Rows.ToList();
		// Actual
		var user = context.Get<User>("User");
		var accounts = user.Accounts;
		// Assert
		accounts.Count.ShouldBe(expectedRows.Count);
		for (int i = 0; i < expectedRows.Count; i++)
		{
			var row = expectedRows[i];
			var match = accounts.FirstOrDefault(a => a.AccountName == row["AccountName"]);
			match.ShouldNotBeNull();
			match!.AccountType.ShouldBe(row["AccountType"]);
			if (dataTable.Header.Contains("CachedBalance"))
			{
				match.CachedBalance.ShouldBe(decimal.Parse(row["CachedBalance"]));
			}
		}
	}
}
