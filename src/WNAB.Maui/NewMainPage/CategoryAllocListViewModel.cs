using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using WNAB.Logic;

namespace WNAB.Maui.NewMainPage;

public class CategoryAllocListViewModel : ObservableObject
{

    List<CategoryAllocComponentViewModel> list;
    void AddAlloc(CategoryAllocationResponse car)
    {
        foreach (var alloc in car.allocations)
        {   
            // send request and get back the response
            //CategoryAllocation allocview = CategoryAllocationManagementService.GetAsync(alloc.Id);
            list.Add(new CategoryAllocComponentViewModel(allocview));
        }

    }



}
