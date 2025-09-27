using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

// LLM-Dev:v2 ViewModel listing users via shared Logic service; UI stays thin.
public sealed partial class UsersViewModel : ObservableObject
{
    private readonly UserManagementService _users;

    public ObservableCollection<UserItem> Users { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public UsersViewModel(UserManagementService users)
    {
        _users = users;
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
}

public sealed record UserItem(int Id, string First, string Last, string Email);