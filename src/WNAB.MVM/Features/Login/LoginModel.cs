using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Storage;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Login feature.
/// Handles user validation, authentication, and session persistence.
/// </summary>
public partial class LoginModel : ObservableObject
{
    private readonly UserManagementService _userService;

    [ObservableProperty]
    private string? userId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LoginModel(UserManagementService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Result of login operation with details for UI presentation.
    /// </summary>
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UserFullName { get; set; }
    }

    /// <summary>
    /// Validates that the user ID input is properly formatted.
    /// </summary>
    /// <returns>True if valid, false otherwise with status message set.</returns>
    public bool ValidateUserIdInput()
    {
        var id = (UserId ?? string.Empty).Trim();
        
        if (string.IsNullOrWhiteSpace(id))
        {
            StatusMessage = "Please enter a user ID.";
            return false;
        }

        if (!int.TryParse(id, out _))
        {
            StatusMessage = "User ID must be a number.";
            return false;
        }

        StatusMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates that a user with the given ID exists in the database.
    /// </summary>
    /// <returns>LoginResult with user details if found, or error message if not.</returns>
    public async Task<LoginResult> ValidateUserExistsAsync(int userIdInt)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userIdInt);
            
            if (user is null)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "The specified user ID does not exist in the database or is inactive."
                };
            }

            return new LoginResult
            {
                Success = true,
                Message = "User validated successfully.",
                UserFullName = $"{user.FirstName} {user.LastName}"
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = $"Failed to validate user: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Saves the user ID to secure storage for session persistence.
    /// </summary>
    public async Task SaveUserSessionAsync(string userId)
    {
        await SecureStorage.Default.SetAsync("userId", userId);
    }

    /// <summary>
    /// Orchestrates the complete login flow: validation, database check, and session save.
    /// </summary>
    /// <returns>LoginResult with success status and appropriate messages.</returns>
    public async Task<LoginResult> PerformLoginAsync()
    {
        if (IsBusy)
        {
            return new LoginResult
            {
                Success = false,
                Message = "Login already in progress."
            };
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Validating...";

            // Step 1: Validate input format
            if (!ValidateUserIdInput())
            {
                return new LoginResult
                {
                    Success = false,
                    Message = StatusMessage
                };
            }

            var id = (UserId ?? string.Empty).Trim();
            var userIdInt = int.Parse(id);

            // Step 2: Validate user exists in database
            StatusMessage = "Checking user...";
            var validationResult = await ValidateUserExistsAsync(userIdInt);
            
            if (!validationResult.Success)
            {
                StatusMessage = validationResult.Message;
                return validationResult;
            }

            // Step 3: Save session
            StatusMessage = "Saving session...";
            await SaveUserSessionAsync(id);

            StatusMessage = "Login successful!";
            return new LoginResult
            {
                Success = true,
                Message = $"Welcome {validationResult.UserFullName}!",
                UserFullName = validationResult.UserFullName
            };
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login error: {ex.Message}";
            return new LoginResult
            {
                Success = false,
                Message = $"An unexpected error occurred: {ex.Message}"
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
