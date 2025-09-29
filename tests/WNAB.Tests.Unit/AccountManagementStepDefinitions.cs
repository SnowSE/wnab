using System;
using System.Linq;
using Reqnroll;
using WNAB.Logic; // LLM-Dev: Use services to create DTO records
using WNAB.Logic.Data;
using Shouldly;

namespace WNAB.Tests.Unit;

public partial class StepDefinitions
{
	[Given(@"the following account for user ""(.*)""")]
	public void Giventhefollowingaccountforuser(string email, DataTable dataTable)
	{
		// LLM-Dev:v2 Build AccountRecord and stage only the record and raw row data for later creation.
		var row = dataTable.Rows.Single();
		// LLM-Dev:v2 Use static method; no HttpClient or service instance needed
		var accountRecord = AccountManagementService.CreateAccountRecord(row["AccountName"]);
		var recordKey = $"AccountRecords:{email.ToLower()}";
		var stagedRecords = context.ContainsKey(recordKey) ? context.Get<List<AccountRecord>>(recordKey) : new List<AccountRecord>();
		stagedRecords.Add(accountRecord);
		context[recordKey] = stagedRecords;

		// LLM-Dev:v2 Stage full row data for later entity creation in a When-step (no extra class)
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
		context[rowsKey] = stagedRows;
	}

	// LLM-Dev:v3 Add Given alias so this binds when "And" inherits Given in the feature
	[Given(@"I create the accounts")]
	[When(@"I create the accounts")]
	public void WhenICreateTheAccounts()
	{
		// LLM-Dev:v2 Create Account entities from staged rows for the current user; reuse user creation step if needed.
		if (!context.ContainsKey("User"))
		{
			// Reuse the usermanagement step within this partial class
			WhenICreateTheUser();
		}
		var user = context.Get<User>("User");
		var emailKey = user.Email.ToLower();
		var rowsKey = $"StagedAccountRows:{emailKey}";
		var stagedRows = context.ContainsKey(rowsKey)
			? context.Get<List<Dictionary<string, string>>>(rowsKey)
			: new List<Dictionary<string, string>>();
		var accountsKey = $"Accounts:{emailKey}";
		var accounts = context.ContainsKey(accountsKey) ? context.Get<List<Account>>(accountsKey) : new List<Account>();
		int nextAccountId = accounts.Any() ? accounts.Max(a => a.Id) + 1 : 1;
		foreach (var r in stagedRows)
		{
			var acct = new Account
			{
				Id = nextAccountId++,
				AccountName = r["AccountName"],
				AccountType = r["AccountType"],
				CachedBalance = decimal.Parse(r["OpeningBalance"]),
				CachedBalanceDate = DateTime.UtcNow,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				UserId = user.Id,
				User = user
			};
			accounts.Add(acct);
		}
		context[accountsKey] = accounts;
		user.Accounts = accounts;
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
