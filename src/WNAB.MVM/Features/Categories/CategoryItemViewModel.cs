using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Data;

namespace WNAB.MVM;

/// <summary>
/// ViewModel wrapper for individual Category items to support inline editing.
/// Maintains edit state and temporary edit values for Name and Color.
/// </summary>
public partial class CategoryItemViewModel : ObservableObject
{
    private readonly Category _category;

    /// <summary>
    /// The underlying Category model object.
    /// </summary>
    public Category Category => _category;

    // Expose Category properties for display
    public int Id => _category.Id;
    public string Name => _category.Name;
    public string? Color => _category.Color;
    public bool IsActive => _category.IsActive;
    public DateTime CreatedAt => _category.CreatedAt;
    public DateTime UpdatedAt => _category.UpdatedAt;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string editName = string.Empty;

    [ObservableProperty]
    private string editColor = "#ef4444";

    /// <summary>
    /// Available color options for the color picker (static).
    /// </summary>
    public static List<string> ColorOptions { get; } = new()
    {
        "#ef4444", // red
        "#f59e0b", // orange
        "#10b981", // green
        "#3b82f6", // blue
        "#6366f1", // indigo
        "#7c3aed", // purple
        "#ec4899", // pink
        "#14b8a6"  // teal
    };

    /// <summary>
    /// Instance property to access color options for binding in XAML.
    /// </summary>
    public List<string> AvailableColors => ColorOptions;

    public CategoryItemViewModel(Category category)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
    }

    /// <summary>
    /// Select color command for inline editing color picker.
    /// </summary>
    [RelayCommand]
    private void SelectColor(string color)
    {
        EditColor = color;
    }

    /// <summary>
    /// Start editing mode - stores current values for cancel functionality.
    /// </summary>
    public void StartEditing()
    {
        EditName = _category.Name;
        EditColor = _category.Color ?? "#ef4444";
        IsEditing = true;
    }

    /// <summary>
    /// Cancel editing mode - discards changes.
    /// </summary>
    public void CancelEditing()
    {
        IsEditing = false;
        EditName = string.Empty;
        EditColor = "#ef4444";
    }

    /// <summary>
    /// Apply saved changes to the underlying Category model.
    /// </summary>
    public void ApplyChanges()
    {
        _category.Name = EditName;
        _category.Color = EditColor;
        IsEditing = false;
        
        // Notify UI of property changes
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Color));
    }
}
