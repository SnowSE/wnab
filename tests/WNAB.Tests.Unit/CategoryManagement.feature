Feature: Category Management

  In order to organize my spending
  As a user of the WNAB system  
  I want to create and manage spending categories

  Scenario: Create a single category
    Given the created user
      | Id | FirstName | LastName | Email                |
      | 1  | John      | Doe      | john.doe@example.com |
    And the following category
      | CategoryName |
      | Groceries    |
    When I create the category
    Then I should have the following category in the system
      | CategoryName |
      | Groceries    |

  Scenario: Create multiple categories
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 2  | Jane      | Smith    | jane.smith@example.com |
    And the following categories
      | CategoryName  |
      | Groceries     |
      | Personal Care |
      | Utilities     |
    When I create the categories
    Then I should have the following categories in the system
      | CategoryName  |
      | Groceries     |
      | Personal Care |
      | Utilities     |

  Scenario: Create categories for budget planning
    Given the created user
      | Id | FirstName | LastName | Email                 |
      | 3  | Bob       | Johnson  | bob.j@example.io      |
    And the following categories
      | CategoryName |
      | Dining       |
      | Fuel         |
      | Entertainment|
    When I create the categories
    Then I should have the following categories in the system
      | CategoryName |
      | Dining       |
      | Fuel         |
      | Entertainment|