Feature: Account Management

  In order to track where my money is stored
  As a user of the WNAB system
  I want to create financial accounts for a user


  Scenario: Create a user and a checking account
    Given the created user
      | Id | FirstName | LastName | Email                  |
      | 1  | Bob       | Johnson  | bob.j@example.io       |
    And the following account for user
      | AccountName      | AccountType |
      | EverydayChecking | Checking    |
    When I create the accounts
    Then the user should have the following accounts
      | AccountName      | AccountType |
      | EverydayChecking | Checking    | 

