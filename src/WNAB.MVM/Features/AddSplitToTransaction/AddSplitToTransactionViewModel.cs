using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WNAB.MVM;

public partial class AddSplitToTransactionViewModel : ObservableObject
{
    public AddSplitToTransactionModel Model { get; }

    public event EventHandler? RequestClose;

    public AddSplitToTransactionViewModel(AddSplitToTransactionModel model)
    {
        Model = model;
    }

    public async Task InitializeAsync()
    {
        await Model.InitializeAsync();
    }

    public async Task LoadTransactionAsync(int transactionId)
    {
  await Model.LoadTransactionAsync(transactionId);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
 }

    [RelayCommand]
    private async Task Save()
    {
        var (success, message) = await Model.CreateSplitAsync();

        if (success)
        {
       Model.Clear();
            RequestClose?.Invoke(this, EventArgs.Empty);
   }
    }
}
