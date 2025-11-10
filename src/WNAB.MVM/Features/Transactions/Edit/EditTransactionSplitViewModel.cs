using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

public partial class EditTransactionSplitViewModel : ObservableObject
{
    public EditTransactionSplitModel Model { get; }

    public event EventHandler? RequestClose;

    public EditTransactionSplitViewModel(EditTransactionSplitModel model)
  {
     Model = model;
    }

    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    public async Task LoadSplitAsync(int id)
    {
      await Model.LoadSplitAsync(id);
    }

    [RelayCommand]
    private void Cancel()
    {
     RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
        var (success, message) = await Model.UpdateSplitAsync();

        if (success)
        {
            Model.Clear();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
  }
}
