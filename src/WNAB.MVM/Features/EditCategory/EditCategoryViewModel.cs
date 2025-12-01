using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for Edit Category popup - thin coordination layer between View and Model.
/// Handles UI-specific concerns like popup closing, delegates business logic to Model.
/// </summary>
public partial class EditCategoryViewModel : ObservableObject
{
    private readonly IMVMPopupService _popupService;

    public event EventHandler? RequestClose;

    public EditCategoryModel Model { get; }

    public EditCategoryViewModel(EditCategoryModel model, IMVMPopupService popupService)
    {
        Model = model;
        _popupService = popupService;
    }

    /// <summary>
    /// Initialize the ViewModel with category data.
    /// </summary>
    public void Initialize(int id, string name, string? color, bool isActive)
    {
        Model.Initialize(id, name, color, isActive);
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
    /// Update command - delegates to Model then closes popup on success.
    /// </summary>
    [RelayCommand]
    private async Task UpdateAsync()
    {
        var success = await Model.UpdateCategoryAsync();
        if (!success)
        {
            await _popupService.DisplayAlertAsync("Error", Model.ErrorMessage ?? "Something went wrong and we were unable to update the category.");
            return;
        }

        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
