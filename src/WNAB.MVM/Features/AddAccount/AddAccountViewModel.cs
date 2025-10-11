using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Add Account popup - thin coordination layer between View and Model.
/// Handles UI-specific concerns like popup closing, delegates business logic to Model.
/// </summary>
public partial class AddAccountViewModel : ObservableObject
{
    public event EventHandler? RequestClose;

    public AddAccountModel Model { get; }

    public AddAccountViewModel(AddAccountModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Initialize the ViewModel by delegating to the Model.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    /// <summary>
    /// Close command - pure UI coordination.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Create command - delegates to Model then closes popup on success.
    /// </summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        var success = await Model.CreateAccountAsync();
        if (success)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
