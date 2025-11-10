using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

public partial class EditTransactionViewModel : ObservableObject
{
    public EditTransactionModel Model { get; }

    public event EventHandler? RequestClose;

    public EditTransactionViewModel(EditTransactionModel model)
    {
    Model = model;
    }

    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    public async Task LoadTransactionAsync(int id)
 {
        await Model.LoadTransactionAsync(id);
    }

    [RelayCommand]
    private void Cancel()
    {
 RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Save()
    {
     var (success, message) = await Model.UpdateTransactionAsync();

        if (success)
   {
            Model.Clear();
 RequestClose?.Invoke(this, EventArgs.Empty);
  }
  }
}
