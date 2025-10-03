Feature: TransactionEntry

Shows how a user can enter a transaction in the application.
# LLM-Dev v6.0: Updated to separate transaction creation from split creation following service patterns.
# Given transaction creates basic transaction info, Given splits creates split records, When combines them.

@tag1
Scenario: Buy groceries
	Given the created user
		| Id | FirstName | LastName | Email                |
		| 1  | John      | Doe      | john.doe@example.com |
	And the following account for user "john.doe@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And I create the accounts
	And the following category for user "john.doe@example.com"
		| CategoryName |
		| Groceries    |
	And the following transaction
		| Date      | Payee   | Memo     | Amount |
		| 9/10/2025 | Walmart | Buy food | 150.00 |
	And the following transaction splits
		| Category  | Amount |
		| Groceries | 150.00 |
	When I enter the transaction with split
	Then I should have the following transaction entry
		| TransactionDate | Amount | Memo     |
		| 9/10/2025       | 150.00 | Buy food |
	And I should have the following transaction splits
		| Category  | Amount |
		| Groceries | 150.00 |

Scenario: Buy groceries and personal care
	Given the created user
		| Id | FirstName | LastName | Email                  |
		| 2  | Jane      | Smith    | jane.smith@example.com |
	And the following account for user "jane.smith@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And I create the accounts
	And the following categories for user "jane.smith@example.com"
		| CategoryName  |
		| Groceries     |
		| Personal Care |
	And the following transaction
		| Date      | Payee   | Memo        | Amount |
		| 9/10/2025 | Walmart | Walmart run | 150.00 |
	And the following transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |
	When I enter the transaction with split
	Then I should have the following transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |


	