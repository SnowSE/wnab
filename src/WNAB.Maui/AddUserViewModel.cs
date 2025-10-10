using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

public partial class AddUserViewModel : ObservableObject
{
    private readonly UserManagementService _users;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

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
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email))
            return;

        var record = new UserRecord(FirstName, LastName, Email);
        await _users.CreateUserAsync(record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}