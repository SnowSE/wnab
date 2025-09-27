using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;

namespace WNAB.Maui;

public partial class AddUserViewModel : ObservableObject
{
    private readonly UserManagementService _users;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    public AddUserViewModel(UserManagementService users)
    {
        _users = users;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
            return;

        var record = UserManagementService.CreateUserRecord(Name, Email);
        await _users.CreateUserAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}