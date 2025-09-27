using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic;
using WNAB.Logic.Data;

namespace WNAB.Maui;

public partial class AccountsViewModel : ObservableObject
{
    private readonly AccountManagementService _accounts;

    public ObservableCollection<AccountItem> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private int userId;

    public AccountsViewModel(AccountManagementService accounts)
    {
        _accounts = accounts;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Items.Clear();
            var list = await _accounts.GetAccountsForUserAsync(UserId);
            foreach (var a in list)
                Items.Add(new AccountItem(a.Id, a.AccountName, a.AccountType, a.CachedBalance));
        }
        finally { IsBusy = false; }
    }
}

public sealed record AccountItem(int Id, string Name, string Type, decimal Balance);