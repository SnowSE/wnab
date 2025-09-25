Feature: Account Management

  In order to track where my money is stored
  As a user of the WNAB system
  I want to create financial accounts for a user

  # LLM-Dev: Step definitions pending. Will rely on user creation + account creation steps.

  Background:
    Given the system has no existing users

  Scenario: Create a user and a checking account
    When I create the following user
      | FirstName | LastName | Email                  |
      | Bob       | Johnson  | bob.j@example.io       |
    And I create the following account for user "bob.j@example.io"
      | AccountName      | AccountType | OpeningBalance |
      | EverydayChecking | bank        | 1500.00        |
    Then the user "bob.j@example.io" should have the following accounts
      | AccountName      | AccountType | CachedBalance |
      | EverydayChecking | bank        | 1500.00       |

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
