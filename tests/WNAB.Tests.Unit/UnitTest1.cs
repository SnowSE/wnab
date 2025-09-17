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

	
}