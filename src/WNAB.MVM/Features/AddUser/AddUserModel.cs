using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.SharedDTOs;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Add User feature.
/// Handles form state, validation, and user creation operations.
/// </summary>
public partial class AddUserModel : ObservableObject
{
    private readonly UserManagementService _users;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public AddUserModel(UserManagementService users)
    {
        _users = users;
    }

    /// <summary>
    /// Validates that all required fields have values.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(FirstName) 
            && !string.IsNullOrWhiteSpace(LastName) 
            && !string.IsNullOrWhiteSpace(Email);
    }

    /// <summary>
    /// Creates a new user with the current form data.
    /// Returns the created user ID on success, or -1 on validation failure.
    /// </summary>
    public async Task<int> CreateUserAsync(CancellationToken ct = default)
    {
        ErrorMessage = string.Empty;

        if (!IsValid())
        {
            ErrorMessage = "All fields are required";
            return -1;
        }

        try
        {
            IsBusy = true;
            var record = new UserRecord(FirstName, LastName, Email);
            var userId = await _users.CreateUserAsync(record, ct);
            ClearForm();
            return userId;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating user: {ex.Message}";
            return -1;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears all form fields and error messages.
    /// </summary>
    public void ClearForm()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        ErrorMessage = string.Empty;
    }
}
