Feature: CategoryEntry

Shows how a user can manage categories in the application.

@tag1
Scenario: Empty test passes
	Given I have an empty test
	When I run the test
	Then the test should pass

Scenario: Create a transportation category
	Given a category with userId 1 and name "Transportation"
	When I enter the category
	Then I should have my category with userId 1 and name "Transportation"


