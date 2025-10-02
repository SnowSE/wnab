namespace WNAB.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// LLM-Dev:v2 Register routes for programmatic navigation without flyout
		Routing.RegisterRoute("Categories", typeof(CategoriesPage));
		Routing.RegisterRoute("Accounts", typeof(AccountsPage));
		Routing.RegisterRoute("Transactions", typeof(TransactionsPage));
		Routing.RegisterRoute("Users", typeof(UsersPage));
		Routing.RegisterRoute("Login", typeof(LoginPage));
	}
}
