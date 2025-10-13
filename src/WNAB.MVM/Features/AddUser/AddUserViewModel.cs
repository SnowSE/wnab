using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Add User popup - thin coordination layer between View and Model.
/// Handles UI-specific concerns like popup lifecycle, delegates business logic to Model.
/// </summary>
public partial class AddUserViewModel : ObservableObject
{
    public event EventHandler? RequestClose;

    public AddUserModel Model { get; }

    public AddUserViewModel(AddUserModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Close command - pure UI coordination for closing the popup.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Model.ClearForm();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Create command - delegates to Model then handles UI concerns.
    /// </summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        var userId = await Model.CreateUserAsync();
        if (userId > 0)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        // If validation failed or error occurred, Model will have ErrorMessage set
        // and the popup stays open for user to correct
    }
}
