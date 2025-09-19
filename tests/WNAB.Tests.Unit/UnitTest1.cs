using Xunit;

namespace WNAB.Tests.Unit;

public class LogicTests
{
	//Tests for unit-value

		// given a monthly income, a plan end date, and no saved, produce the right unit-value
			// arrange:
				// make a new person
				// give them an income value
				// give them zero savings
				// give an end date
			// act:
				// call the get-unit-value function
			// assert:
				// the unit-value should be equivalent to the income value, but use the number and not an equivalent
		

		// given no monthly income, a plan end date, and some saved money, produce the right unit-value
			// arrange:
				// make a new person
				// give them zero income value
				// give them some savings
				// give an end date
			// act:
			 	// call the get-unit-value function
			// assert:
				// the unit value should be the same as dividing the savings by the (time remaining)/unit time
		

		// given both monthly income and saved money, and a plan end date, produce the right unit-value
			// arrange:
				// make a person
				// give them an income
				// give them some savings
				// give an end date
			// act:
				// call the get-unit-value function
			// assert:
				// the correct unit value is what we got

	// Tests for transactions playing into calculations

		// given some transactions, figure out if someone went over their budget
			// arrange:
				// make a person
				// add income
				// add end date
				// add several transactions
			// act:
				// call the function for analyzing the transactions
			// assert:
				// that the call returns the right information, namely
					// whether the user is going over budget this unit-time
					// how much they have left on the current budget for this unit time

	
	// --- Business Logic Test Pseudocode (Revised for WNAB Model) ---

	// Test: Creating a Transaction with a single category creates one TransactionSplit
	// [Fact]
	// public void AddTransaction_SingleCategory_CreatesOneTransactionSplit()
	// {
	//     // Arrange: Create a Transaction for a User/Account with a single Category
	//     // Act: Add the Transaction
	//     // Assert: One TransactionSplit exists, linked to the Transaction and Category
	// }

	// Test: Creating a Transaction with multiple categories creates multiple TransactionSplits
	// [Fact]
	// public void AddTransaction_MultipleCategories_CreatesMultipleTransactionSplits()
	// {
	//     // Arrange: Create a Transaction for a User/Account with splits across multiple Categories
	//     // Act: Add the Transaction
	//     // Assert: Each split is a TransactionSplit, all linked to the Transaction
	// }

	// Test: Monthly spending per Category is calculated correctly (CategoryAllocation)
	// [Fact]
	// public void GetCategoryMonthlySpending_ReturnsCorrectTotals_NoCrossover()
	// {
	//     // Arrange: Add Transactions and TransactionSplits in different months and categories
	//     // Act: Query monthly spending per Category
	//     // Assert: Each month's total only includes TransactionSplits from that month/category
	// }

	// Test: Deleting a Transaction removes its TransactionSplits
	// [Fact]
	// public void DeleteTransaction_RemovesTransactionSplits()
	// {
	//     // Arrange: Add a Transaction with splits
	//     // Act: Delete the Transaction
	//     // Assert: All TransactionSplits for that Transaction are removed
	// }

	// Test: Editing a Transaction updates its TransactionSplits
	// [Fact]
	// public void EditTransaction_UpdatesTransactionSplits()
	// {
	//     // Arrange: Add a Transaction, then change its splits (amount/category)
	//     // Act: Update the Transaction
	//     // Assert: TransactionSplits reflect the new values
	// }

	// Test: CategoryAllocation enforces unique (CategoryId, Year, Month) per User
	// [Fact]
	// public void AddCategoryAllocation_EnforcesUniquePerMonthCategoryUser()
	// {
	//     // Arrange: Add a CategoryAllocation for a User/Category/Month/Year
	//     // Act: Try to add another for the same tuple
	//     // Assert: Second insert fails or is rejected
	// }
	// --- End Business Logic Test Pseudocode ---
}