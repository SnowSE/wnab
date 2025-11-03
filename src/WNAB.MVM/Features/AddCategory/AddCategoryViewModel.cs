using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Add Category popup - thin coordination layer between View and Model.
/// Handles UI-specific concerns like popup closing, delegates business logic to Model.
/// </summary>
public partial class AddCategoryViewModel : ObservableObject
{
    public event EventHandler? RequestClose;

    public AddCategoryModel Model { get; }

    public AddCategoryViewModel(AddCategoryModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Close command - pure UI concern to dismiss the popup.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Select color command - updates the selected color in the model.
    /// </summary>
    [RelayCommand]
    private void SelectColor(string color)
    {
        Model.SelectedColor = color;
    }

    /// <summary>
    /// Create command - delegates to Model then closes popup on success.
    /// </summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        var success = await Model.CreateCategoryAsync();
        if (!success)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Something went wrong and we were unable to create the category.", "OK");
        }

        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
