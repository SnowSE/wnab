Feature: Category Allocation

  In order to plan my spending
  As a user of the WNAB system
  I want to allocate budgeted amounts to my categories

  # LLM-Dev: Step definitions pending. Requires steps for creating categories and allocations.

  # LLM-Dev: Removed Background that cleared all users. Aligning with TransactionEntry.feature approach (scenarios declare only the data they need).
  # LLM-Dev:v2 New step phrases: "Given the created user", "the following category/categories for user", and "When I allocate the following amounts".

  Scenario: Create user, category, and allocate budget for current month
    Given the created user
      | FirstName | LastName | Email                |
      | Carol     | Wang     | carol.w@example.io   |
    And the following category for user "carol.w@example.io"
      | CategoryName |
      | Groceries    |
    When I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |
    Then I should have the following category allocations for user "carol.w@example.io"
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |

  Scenario: Multiple allocations across months
    Given the created user
      | FirstName | LastName | Email                |
      | Dana      | Lee      | dana.lee@example.io  |
    And the following category for user "dana.lee@example.io"
      | CategoryName |
      | Utilities    |
    When I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |
    Then I should have the following category allocations for user "dana.lee@example.io"
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |

  Scenario: Allocate budget to multiple categories
    Given the created user
      | FirstName | LastName | Email                 |
      | Fiona     | Patel    | fiona.p@example.io    |
    And the following categories for user "fiona.p@example.io"
      | CategoryName |
      | Dining       |
      | Fuel         |
    When I allocate the following amounts
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
    Then I should have the following category allocations for user "fiona.p@example.io"
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
