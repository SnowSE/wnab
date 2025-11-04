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
		
		// Subscribe to Navigating event to check auth before navigation completes
		this.Navigating += OnNavigating;
	}

	private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
	{
		var isAuthenticated = await _authService.IsAuthenticatedAsync();
		var targetLocation = e.Target.Location.OriginalString;

		// If navigating to Landing page while authenticated, redirect to MainPage
		if (targetLocation == "//Landing" && isAuthenticated)
		{
			e.Cancel(); // Cancel the navigation to Landing
			// Defer the redirect to MainPage
			Dispatcher.Dispatch(async () =>
			{
				await Shell.Current.GoToAsync("//MainPage");
			});
			return;
		}

		// If navigating to any protected page while NOT authenticated, redirect to Landing
		var protectedPages = new[] { "//MainPage", "//Categories", "//Accounts", "//Transactions" };
		if (protectedPages.Contains(targetLocation) && !isAuthenticated)
		{
			e.Cancel(); // Cancel the navigation to protected page
			// Defer the redirect to Landing
			Dispatcher.Dispatch(async () =>
			{
				await Shell.Current.GoToAsync("//Landing");
			});
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
