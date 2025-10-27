Feature: Transaction Entry

  In order to track my spending
  As a user of the WNAB system
  I want to create transaction entries

  @tag1
  Scenario: Create a simple transaction
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 1  | John      | Doe      | john.doe@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the following transactions
      | Date      | Payee        | Amount |
      | 9/10/2025 | Test Store   | 150.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount |
      | 9/10/2025       | 150.00 |

  Scenario: Create multiple transactions
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 2  | Jane      | Smith    | jane.smith@example.com |
    And the created accounts
      | AccountName |
      | Checking    |
    And the following transactions
      | Date      | Payee           | Amount |
      | 9/10/2025 | Grocery Store   | 150.00 |
      | 9/11/2025 | Gas Station     | 75.50  |
      | 9/12/2025 | Restaurant      | 200.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount |
      | 9/10/2025       | 150.00 |
      | 9/11/2025       | 75.50  |
      | 9/12/2025       | 200.00 |
@tag1
Scenario: Buy groceries
	Given the created user
		| FirstName | LastName | Email                |
		| John      | Doe      | john.doe@example.com |
	And the created account for user "john.doe@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And the created category for user "john.doe@example.com"
		| CategoryName |
		| Groceries    |
	And the following budget allocation for September 2025
		| CategoryName | BudgetedAmount |
		| Groceries    | 500.00         |
	And the following transaction
		| Date      | Payee   | Memo     | Amount |
		| 9/10/2025 | Walmart | Buy food | 150.00 |
	When I enter the transaction with split
		| Category  | Amount |
		| Groceries | 150.00 |
	Then I should have the following transaction entry
		| TransactionDate | Amount | Memo     |
		| 9/10/2025       | 150.00 | Buy food |
	And I should have the following transaction splits
		| Category  | Amount |
		| Groceries | 150.00 |

Scenario: Buy groceries and personal care
	Given the created user
		| FirstName | LastName | Email                |
		| Jane      | Smith    | jane.smith@example.com |
	And the created account for user "jane.smith@example.com"
		| AccountName     | AccountType | OpeningBalance |
		| Checking        | Checking    | 1000.00        |
	And the created categories for user "jane.smith@example.com"
		| CategoryName  |
		| Groceries     |
		| Personal Care |
	And the following budget allocation for September 2025
		| CategoryName  | BudgetedAmount |
		| Groceries     | 500.00         |
		| Personal Care | 200.00         |
	And the following transaction
		| Date      | Payee   | Memo        | Amount |
		| 9/10/2025 | Walmart | Walmart run | 150.00 |
	When I enter the transaction with split
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |
	Then I should have the following transaction splits
		| Category      | Amount |
		| Groceries     | 100.00 |
		| Personal Care | 50.00  |
  Scenario: Create transaction with different amounts
    Given the created user
      | Id | FirstName | LastName | Email                 |
      | 3  | Bob       | Johnson  | bob.j@example.io      |
    And the created accounts
      | AccountName |
      | Savings     |
    And the following transactions
      | Date      | Payee       | Amount  |
      | 9/15/2025 | Bank        | 1000.00 |
    When I create the transactions
    Then I should have the following transaction entries
      | TransactionDate | Amount  |
      | 9/15/2025       | 1000.00 |

Scenario: Cannot create transaction without budget allocation
	Given the created user
		| FirstName | LastName | Email                  |
		| Bob       | Johnson  | bob.johnson@example.com |
	And the created account for user "bob.johnson@example.com"
		| AccountName | AccountType | OpeningBalance |
		| Checking    | Checking    | 1000.00        |
	And the created category for user "bob.johnson@example.com"
		| CategoryName |
		| Groceries    |
	And the following transaction
		| Date      | Payee   | Memo     | Amount |
		| 9/10/2025 | Walmart | Buy food | 150.00 |
	When I attempt to enter the transaction with split
		| Category  | Amount |
		| Groceries | 150.00 |
	Then the transaction creation should fail with message "No budget allocation found for category 'Groceries' in September 2025"


	