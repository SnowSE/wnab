namespace WNAB.Maui;

public partial class AppShell : Shell
{
	private readonly IAuthenticationService _authService;

	public AppShell()
	{
		InitializeComponent();
		
		// LLM-Dev:v4 Register routes for programmatic navigation, including NewMainPage
		Routing.RegisterRoute("Categories", typeof(CategoriesPage));
		Routing.RegisterRoute("Accounts", typeof(AccountsPage));
		Routing.RegisterRoute("Transactions", typeof(TransactionsPage));
		Routing.RegisterRoute("Landing", typeof(LandingPage));

		_authService = ServiceHelper.GetService<IAuthenticationService>();
		
		// Set initial page based on authentication state
		_ = InitializeNavigationAsync();
	}

	private async Task InitializeNavigationAsync()
	{
		var isAuthenticated = await _authService.IsAuthenticatedAsync();
		if (!isAuthenticated)
		{
			await GoToAsync("//Landing");
		}
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		var confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
		if (confirm)
		{
			await _authService.LogoutAsync();
			// Navigate to landing page after logout
			await Shell.Current.GoToAsync("//Landing");
		}
	}
}
