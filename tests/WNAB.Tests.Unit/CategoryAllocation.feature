Feature: Category Allocation

  In order to plan my spending
  As a user of the WNAB system
  I want to allocate budgeted amounts to my categories

  Scenario: Allocate budget to existing category for current month
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 4  | Carol     | Wang     | carol.w@example.io   |
    And the existing categories
      | CategoryName |
      | Groceries    |
    When I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |
    Then I should have the following category allocations
      | Category  | Month | Year | BudgetedAmount |
      | Groceries | 9     | 2025 | 400.00         |

  Scenario: Multiple allocations across months for existing category
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 5  | Dana      | Lee      | dana.lee@example.io  |
    And the existing categories
      | CategoryName |
      | Utilities    |
    When I allocate the following amounts
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |
    Then I should have the following category allocations
      | Category  | Month | Year | BudgetedAmount |
      | Utilities | 8     | 2025 | 120.00         |
      | Utilities | 9     | 2025 | 125.50         |
      | Utilities | 10    | 2025 | 130.00         |

  Scenario: Allocate budget to multiple existing categories
    Given the created user
      | Id | FirstName | LastName | Email                 |
      | 6  | Fiona     | Patel    | fiona.p@example.io    |
    And the existing categories
      | CategoryName |
      | Dining       |
      | Fuel         |
    When I allocate the following amounts
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
    Then I should have the following category allocations
      | Category | Month | Year | BudgetedAmount |
      | Dining   | 9     | 2025 | 300.00         |
      | Fuel     | 9     | 2025 | 150.00         |
