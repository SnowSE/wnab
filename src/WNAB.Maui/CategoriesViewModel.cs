using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WNAB.Logic; // LLM-Dev: Use shared CategoryManagementService

namespace WNAB.Maui;

// LLM-Dev: ViewModel for Categories page. Keeps UI logic out of the view (MVVM).
public sealed partial class CategoriesViewModel : ObservableObject
{
    private readonly CategoryManagementService _service;

    public ObservableCollection<CategoryItem> Categories { get; } = new();

    // AOT/WinRT-friendly property (avoids MVVMTK0045 without requiring C# preview)
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public CategoriesViewModel(CategoryManagementService service)
    {
        _service = service;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            Categories.Clear();
            var items = await _service.GetCategoriesAsync();
            foreach (var c in items)
                Categories.Add(new CategoryItem(c.Id, c.Name));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record CategoryItem(int Id, string Name);