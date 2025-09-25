Feature: User, Account, and Category Allocation Management

  In order to budget effectively
  As a user of the WNAB system
  I want to create users, their accounts, and allocate budget to categories

  # LLM-Dev: The step definitions for these scenarios do not yet exist. They should
  # mirror the style of TransactionEntry steps (table-driven). Each Given/When/Then
  # is phrased to allow straightforward mapping to C# step definitions later.

  Background:
    Given the system has no existing users

  Scenario: Create a new user
    When I create the following user
      | FirstName | LastName | Email                  |
      | Alice     | Smith    | alice.smith@example.io |
    Then I should have the following user in the system
      | FirstName | LastName | Email                  | IsActive |
      | Alice     | Smith    | alice.smith@example.io | true     |

  Scenario: Create a user and a checking account
    When I create the following user
      | FirstName | LastName | Email                  |
      | Bob       | Johnson  | bob.j@example.io       |
    And I create the following account for user "bob.j@example.io"
      | AccountName     | AccountType | OpeningBalance |
      | EverydayChecking| bank        | 1500.00        |
    Then the user "bob.j@example.io" should have the following accounts
      | AccountName      | AccountType | CachedBalance |
      | EverydayChecking | bank        | 1500.00       |

  Scenario: Create user, category, and allocate budget for current month
    When I create the following user
      | FirstName | LastName | Email                |
      | Carol     | Wang     | carol.w@example.io   |
    And I create the following category for user "carol.w@example.io"
      | CategoryName |
      | Groceries    |
    And I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |
    Then I should have the following category allocations for user "carol.w@example.io"
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |

  Scenario: Multiple allocations across months
    When I create the following user
      | FirstName | LastName | Email                |
      | Dana      | Lee      | dana.lee@example.io  |
    And I create the following category for user "dana.lee@example.io"
      | CategoryName |
      | Utilities    |
    And I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |
    Then I should have the following category allocations for user "dana.lee@example.io"
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |

  Scenario: Create two accounts for a user
    When I create the following user
      | FirstName | LastName | Email               |
      | Evan      | Ortiz    | evan.o@example.io   |
    And I create the following accounts for user "evan.o@example.io"
      | AccountName       | AccountType | OpeningBalance |
      | Primary Checking  | bank        | 500.00         |
      | Vacation Savings  | bank        | 2500.00        |
    Then the user "evan.o@example.io" should have the following accounts
      | AccountName       | AccountType | CachedBalance |
      | Primary Checking  | bank        | 500.00        |
      | Vacation Savings  | bank        | 2500.00       |

  Scenario: Allocate budget to multiple categories
    When I create the following user
      | FirstName | LastName | Email                 |
      | Fiona     | Patel    | fiona.p@example.io    |
    And I create the following categories for user "fiona.p@example.io"
      | CategoryName |
      | Dining       |
      | Fuel         |
    And I allocate the following amounts
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
    Then I should have the following category allocations for user "fiona.p@example.io"
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
