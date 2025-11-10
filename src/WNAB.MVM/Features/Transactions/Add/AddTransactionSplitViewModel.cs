using CommunityToolkit.Mvvm.ComponentModel;

namespace WNAB.MVM;

/// <summary>
/// ViewModel for individual transaction split items - thin coordination layer between View and Model.
/// Delegates all business logic to AddTransactionSplitModel.
/// </summary>
public partial class AddTransactionSplitViewModel : ObservableObject
{
    public AddTransactionSplitModel Model { get; }

    public AddTransactionSplitViewModel(AddTransactionSplitModel model)
    {
        Model = model;
    }

    /// <summary>
    /// Convenience constructor that creates a new Model instance.
    /// </summary>
    public AddTransactionSplitViewModel() : this(new AddTransactionSplitModel())
    {
    }
}
