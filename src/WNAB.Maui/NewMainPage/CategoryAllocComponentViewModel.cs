using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using WNAB.Logic;

namespace WNAB.Maui.NewMainPage;

public class CategoryAllocComponentViewModel : ObservableObject
{
    public CategoryAllocComponentViewModel(AllocationTransactionsResponse car)
    {
        // get transaction splits from car.Id => into the field
        transactionSplits = car.transactionsplits;
        // onproperty changed everything.
        OnPropertyChanged(nameof(transactionSplits));
    }


    // a list that holds transactionsplits
    
    List<TransactionSplits> transactionSplits;
    // a field that holds a goal amount

    // a field calculated from transactionsplits (total)

    // a field calculated from goal/total (percentage) for progress bar

    // no buttons.

}
