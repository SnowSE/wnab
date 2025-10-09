using System;
using System.Collections.Generic;
using System.Text;

namespace WNAB.Maui.NewMainPage;

public class CategoryAllocListViewModel
{

    List<CategoryAllocComponentViewModel> list;
    void AddAlloc(CategoryAllocationResponse car)
    {
        list.Add(new CategoryAllocComponentViewModel(car));
    }

}
