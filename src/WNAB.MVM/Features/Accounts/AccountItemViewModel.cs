using CommunityToolkit.Mvvm.ComponentModel;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// ViewModel wrapper for individual Account items to support inline editing.
/// Maintains edit state and temporary edit values for AccountName and AccountType.
/// </summary>
public partial class AccountItemViewModel : ObservableObject
{
    private readonly Account _account;

    /// <summary>
    /// The underlying Account model object.
    /// </summary>
    public Account Account => _account;

    // Expose Account properties for display
    public int Id => _account.Id;
    public string AccountName => _account.AccountName;
    public AccountType AccountType => _account.AccountType;
    public string AccountTypeDisplay => _account.AccountType.ToString();
    public decimal CachedBalance => _account.CachedBalance;
    public DateTime UpdatedAt => _account.UpdatedAt;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string editAccountName = string.Empty;

    [ObservableProperty]
    private string editAccountTypeString = string.Empty;

    public AccountType EditAccountType
    {
        get => Enum.TryParse<AccountType>(EditAccountTypeString, out var result) ? result : AccountType.Checking;
        set => EditAccountTypeString = value.ToString();
    }

    public static string[] AccountTypeOptions => new[] { "Checking", "Savings", "Misc" };

    public AccountItemViewModel(Account account)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
    }

    /// <summary>
    /// Start editing mode - stores current values for cancel functionality.
    /// </summary>
    public void StartEditing()
    {
        EditAccountName = _account.AccountName;
        EditAccountTypeString = _account.AccountType.ToString();
        IsEditing = true;
    }

    /// <summary>
    /// Cancel editing mode - discards changes.
    /// </summary>
    public void CancelEditing()
    {
        IsEditing = false;
        EditAccountName = string.Empty;
        EditAccountTypeString = string.Empty;
    }

    /// <summary>
    /// Apply saved changes to the underlying Account model.
    /// </summary>
    public void ApplyChanges()
    {
        _account.AccountName = EditAccountName;
        _account.AccountType = EditAccountType;
        IsEditing = false;
        
        // Notify UI of property changes
        OnPropertyChanged(nameof(AccountName));
        OnPropertyChanged(nameof(AccountType));
        OnPropertyChanged(nameof(AccountTypeDisplay));
    }
}
