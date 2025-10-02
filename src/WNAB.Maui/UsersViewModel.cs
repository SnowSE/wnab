using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

// LLM-Dev:v2 ViewModel listing users via shared Logic service; UI stays thin.
// LLM-Dev:v3 Added AddUser command and IPopupService injection
public sealed partial class UsersViewModel : ObservableObject
{
    private readonly UserManagementService _users;
    private readonly IPopupService _popupService;

    public ObservableCollection<UserItem> Users { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public UsersViewModel(UserManagementService users, IPopupService popupService)
    {
        _users = users;
        _popupService = popupService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Users.Clear();
            var items = await _users.GetUsersAsync();
            foreach (var u in items)
                Users.Add(new UserItem(u.Id, u.FirstName, u.LastName, u.Email));
        }
        finally { IsBusy = false; }
    }

    // LLM-Dev:v3 Add command to open Add User popup
    [RelayCommand]
    private async Task AddUser()
    {
        await _popupService.ShowAddUserAsync();
        // Refresh the list after popup closes
        await LoadAsync();
    }

    // LLM-Dev:v4 Navigation command to return to home page
    [RelayCommand]
    private async Task NavigateToHome()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

public sealed record UserItem(int Id, string First, string Last, string Email);