// LLM-Dev: Minimal budget UI logic

// Sample data
let categories = [
	{ name: "Groceries", detailsOpen: false, amount: 300 },
	{ name: "Utilities", detailsOpen: false, amount: 100 },
];
let globalAmount = 4000;


const budgetContainer = document.getElementById("budgetcolumn");

// Render daily budget UI
function renderDailyBudgetUI() {
	// Create container
	const dailyBudgetDiv = document.createElement('div');
	dailyBudgetDiv.className = 'budget-container';

	// Title
	const title = document.createElement('h1');
	title.textContent = 'Budget App';
	dailyBudgetDiv.appendChild(title);

	// Income input
	const incomeLabel = document.createElement('label');
	incomeLabel.setAttribute('for', 'income');
	incomeLabel.textContent = 'Monthly Income:';
	dailyBudgetDiv.appendChild(incomeLabel);
	const incomeInput = document.createElement('input');
	incomeInput.type = 'number';
	incomeInput.id = 'income';
	incomeInput.placeholder = 'Enter income';
	incomeInput.min = '0';
	dailyBudgetDiv.appendChild(incomeInput);

	// Expenses input
	const expensesLabel = document.createElement('label');
	expensesLabel.setAttribute('for', 'expenses');
	expensesLabel.textContent = 'Monthly Expenses:';
	dailyBudgetDiv.appendChild(expensesLabel);
	const expensesInput = document.createElement('input');
	expensesInput.type = 'number';
	expensesInput.id = 'expenses';
	expensesInput.placeholder = 'Enter expenses';
	expensesInput.min = '0';
	dailyBudgetDiv.appendChild(expensesInput);

	// Month select
	const monthLabel = document.createElement('label');
	monthLabel.setAttribute('for', 'month');
	monthLabel.textContent = 'Month:';
	dailyBudgetDiv.appendChild(monthLabel);
	const monthSelect = document.createElement('select');
	monthSelect.id = 'month';
	[
		'January','February','March','April','May','June','July','August','September','October','November','December'
	].forEach((m, i) => {
		const opt = document.createElement('option');
		opt.value = i+1;
		opt.textContent = m;
		monthSelect.appendChild(opt);
	});
	dailyBudgetDiv.appendChild(monthSelect);

	// Calculate button
	const calcBtn = document.createElement('button');
	calcBtn.id = 'calcBtn';
	calcBtn.textContent = 'Calculate Daily Budget';
	dailyBudgetDiv.appendChild(calcBtn);

	// Result display
	const resultDiv = document.createElement('div');
	resultDiv.id = 'result';
	resultDiv.className = 'result';
	dailyBudgetDiv.appendChild(resultDiv);

	// Add to top of budget column
	budgetContainer.parentNode.insertBefore(dailyBudgetDiv, budgetContainer);

	// Calculation logic
	function getDaysInMonth(month, year) {
		return new Date(year, month, 0).getDate();
	}
	function calculateDailyBudget() {
		const income = parseFloat(incomeInput.value) || 0;
		const expenses = parseFloat(expensesInput.value) || 0;
		const month = parseInt(monthSelect.value);
		const year = new Date().getFullYear();
		const days = getDaysInMonth(month, year);
		const dailyBudget = (income - expenses) / days;
		return { dailyBudget, days };
	}
	calcBtn.addEventListener('click', function() {
		const { dailyBudget, days } = calculateDailyBudget();
		resultDiv.textContent = `Your daily budget for this month (${days} days): $${dailyBudget.toFixed(2)}`;
	});
}

renderDailyBudgetUI();

function renderBudget() {
	budgetContainer.innerHTML = "";

	// Total amount block
	const totalBlock = document.createElement("div");
	totalBlock.className = "total-block";
	totalAmount = globalAmount;
	for (const cat of categories) {
		totalAmount -= cat.amount;
	}
	totalBlock.textContent = `Total Left: $${totalAmount}`;
	budgetContainer.appendChild(totalBlock);

	// Add category button
	const addBtn = document.createElement("button");
	addBtn.textContent = " + ";
	addBtn.className = "add-category-btn";
	addBtn.onclick = () => {
		const name = prompt("Category name?");
		const amount = parseFloat(prompt("Amount allocated?"));
		if (name) {
			categories.push({ name, detailsOpen: false, amount: amount});
			renderBudget();
		}
	};
	budgetContainer.appendChild(addBtn);


	// Category blocks
	categories.forEach((cat, idx) => {
		const catBlock = document.createElement("div");
		catBlock.className = "category-block";

		// Header
		const header = document.createElement("div");
		header.className = "category-header";
		header.textContent = cat.name + " - $" + cat.amount;
		header.onclick = () => {
			cat.detailsOpen = !cat.detailsOpen;
			renderBudget();
		};
		catBlock.appendChild(header);

		// Details (expandable)
		if (cat.detailsOpen) {
			const details = document.createElement("div");
			details.className = "category-details";
			// Graph placeholders
			const graph1 = document.createElement("div");
			graph1.className = "graph-placeholder";
			graph1.textContent = "Graph 1";
			const graph2 = document.createElement("div");
			graph2.className = "graph-placeholder";
			graph2.textContent = "Graph 2";
			details.appendChild(graph1);
			details.appendChild(graph2);
			catBlock.appendChild(details);
		}

		budgetContainer.appendChild(catBlock);
	});

}

// Initial render

renderBudget();

// Daily budget calculation logic
function getDaysInMonth(month, year) {
	return new Date(year, month, 0).getDate();
}

function calculateDailyBudget() {
	const income = parseFloat(document.getElementById('income').value) || 0;
	const expenses = parseFloat(document.getElementById('expenses').value) || 0;
	const month = parseInt(document.getElementById('month').value);
	const year = new Date().getFullYear();
	const days = getDaysInMonth(month, year);
	const dailyBudget = (income - expenses) / days;
	return { dailyBudget, days };
}

document.getElementById('calcBtn').addEventListener('click', function() {
	const { dailyBudget, days } = calculateDailyBudget();
	const resultDiv = document.getElementById('result');
	resultDiv.textContent = `Your daily budget for this month (${days} days): $${dailyBudget.toFixed(2)}`;
});
