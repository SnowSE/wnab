using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// Business logic and state management for Users feature.
/// Handles data fetching and user list management.
/// </summary>
public partial class UsersModel : ObservableObject
{
    private readonly UserManagementService _users;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Loading...";

    public UsersModel(UserManagementService users)
    {
        _users = users;
    }

    /// <summary>
    /// Load users from the service.
    /// </summary>
    public async Task LoadUsersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading users...";
            Users.Clear();

            var items = await _users.GetUsersAsync();
            foreach (var u in items)
                Users.Add(u);

            StatusMessage = items.Count == 0 ? "No users found" : $"Loaded {items.Count} users";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading users: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Refresh users by reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        await LoadUsersAsync();
    }
}
