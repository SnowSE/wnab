using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for MainPage - thin coordination layer between View and Model.
/// Handles UI-specific concerns like navigation and popups, delegates business logic to Model.
/// </summary>
public partial class MainPageViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;
    private readonly IAuthenticationService _authenticationService;

    public MainPageModel Model { get; }

    public MainPageViewModel(MainPageModel model, IMVMPopupService popupService, IAuthenticationService authenticationService)
    {
        Model = model;
        _popupService = popupService;
        _authenticationService = authenticationService;
        
        // Subscribe to Model's IsSignedIn changes to update computed property
        Model.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Model.IsSignedIn))
            {
                OnPropertyChanged(nameof(LogInSignOutText));
            }
        };
        
        // Initialize sign-in state on construction (fire and forget)
        _ = RefreshUserId();
    }

    /// <summary>
    /// Dynamic auth toolbar text (dependent on Model.IsSignedIn).
    /// </summary>
    public string LogInSignOutText => Model.GetAuthButtonText();

    /// <summary>
    /// Refresh user authentication status - delegates to Model.
    /// </summary>
    [RelayCommand]
    private async Task RefreshUserId()
    {
        await Model.CheckAuthenticationAsync();
    }

    /// <summary>
    /// Sign out command - delegates to Model and shows confirmation.
    /// </summary>
    [RelayCommand]
    private async Task SignOut()
    {
        await Model.SignOutAsync();

        if (Shell.Current is not null)
        {
            // Optional confirmation toast/alert
            await Shell.Current.DisplayAlertAsync("Signed out", "You have been signed out.", "OK");
        }
    }

    /// <summary>
    /// Navigate to Categories page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToCategories()
    {
        await Shell.Current.GoToAsync("Categories");
    }

    /// <summary>
    /// Navigate to Accounts page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAccounts()
    {
        await Shell.Current.GoToAsync("Accounts");
    }

    /// <summary>
    /// Navigate to Transactions page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToTransactions()
    {
        await Shell.Current.GoToAsync("Transactions");
    }

    /// <summary>
    /// Navigate to Users page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToUsers()
    {
        await Shell.Current.GoToAsync("Users");
    }

    /// <summary>
    /// Navigate to Plan Budget page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToPlanBudget()
    {
        await Shell.Current.GoToAsync("PlanBudget");
    }

    /// <summary>
    /// Show login popup and refresh authentication state on success.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToLogin()
    {
        var success = await _authenticationService.LoginAsync();
        if (success)
        {
            // Refresh user display after successful login
            await RefreshUserId();
        }
        else
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlertAsync("Login Failed", "Unable to authenticate. Please try again.", "OK");
            }
        }
    }

    /// <summary>
    /// Unified auth toolbar command - routes to sign-in or sign-out logic based on current state.
    /// </summary>
    [RelayCommand]
    private async Task LogInSignOut()
    {
        if (Model.IsSignedIn)
        {
            await SignOut();
        }
        else
        {
            await NavigateToLogin();
        }
    }
}
