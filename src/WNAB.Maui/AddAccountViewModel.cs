using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;

namespace WNAB.Maui;

public partial class AddAccountViewModel : ObservableObject
{
    private readonly AccountManagementService _accounts;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private int userId;

    [ObservableProperty]
    private string name = string.Empty;

    public AddAccountViewModel(AccountManagementService accounts)
    {
        _accounts = accounts;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (UserId <= 0 || string.IsNullOrWhiteSpace(Name))
            return;

        var record = AccountManagementService.CreateAccountRecord(Name);
        await _accounts.CreateAccountAsync(UserId, record);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}