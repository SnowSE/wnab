Feature: Account Management

  In order to track where my money is stored
  As a user of the WNAB system
  I want to create financial accounts for a user


  Scenario: Create a user and a checking account
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 1  | Bob       | Johnson  | bob.j@example.io       |
    And the following account for user "bob.j@example.io"
      | AccountName      | AccountType | OpeningBalance |
      | EverydayChecking | bank        | 1500.00        |
    And I create the accounts
    Then the user "bob.j@example.io" should have the following accounts
      | AccountName      | AccountType | CachedBalance |
      | EverydayChecking | bank        | 1500.00       |

