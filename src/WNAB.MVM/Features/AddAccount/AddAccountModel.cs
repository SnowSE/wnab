using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Add Account feature.
/// Handles validation, authentication state, and account creation.
/// </summary>
public partial class AddAccountModel : ObservableObject
{
    private readonly AccountManagementService _accounts;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string accountTypeString = "Checking";

    public AccountType AccountType
    {
        get => Enum.TryParse<AccountType>(AccountTypeString, out var result) ? result : AccountType.Checking;
        set => AccountTypeString = value.ToString();
    }

    public static string[] AccountTypeOptions => new[] { "Checking", "Savings", "Misc" };

    [ObservableProperty]
    private string statusMessage = "Ready to create account";

    [ObservableProperty]
    private bool isLoggedIn;

    [ObservableProperty]
    private bool isBusy;

    public AddAccountModel(AccountManagementService accounts, IAuthenticationService authService)
    {
        _accounts = accounts;
        _authService = authService;
    }

    /// <summary>
    /// Initialize the model by checking authentication status.
    /// </summary>
    public async Task InitializeAsync()
    {
        await CheckUserSessionAsync();
    }

    /// <summary>
    /// Check if user is logged in and update authentication state.
    /// </summary>
    public async Task CheckUserSessionAsync()
    {
        try
        {
            IsLoggedIn = await _authService.IsAuthenticatedAsync();
            StatusMessage = IsLoggedIn ? "Ready to create account" : "Please log in first";
        }
        catch
        {
            IsLoggedIn = false;
            StatusMessage = "Error checking login status";
        }
    }

    /// <summary>
    /// Validate the account name input.
    /// </summary>
    /// <returns>True if valid, false otherwise. Updates StatusMessage on failure.</returns>
    public bool ValidateInput()
    {
        if (!IsLoggedIn)
        {
            StatusMessage = "Please log in first to create an account";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Please enter an account name";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Create a new account with the current name.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> CreateAccountAsync()
    {
        await CheckUserSessionAsync();

        if (!ValidateInput())
        {
            return false;
        }

        if (IsBusy) return false;

        try
        {
            IsBusy = true;
            StatusMessage = "Creating account...";
            
            // API derives user from token; pass 0 for UserId
            var record = new AccountRecord(Name, AccountType);
            await _accounts.CreateAccountAsync(record);
            
            StatusMessage = "Account created successfully!";
            ResetForm();
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating account: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Reset the form after successful account creation.
    /// </summary>
    public void ResetForm()
    {
        Name = string.Empty;
        AccountTypeString = "Checking";
    }
}
