using System;

namespace WNAB.Maui;

// LLM-Dev:v1 Created codebehind for the new main page with connection to ViewModel
public partial class NewMainPage : ContentPage
{
    public NewMainPage() : this(ServiceHelper.GetService<NewMainPageViewModel>()) { }
    
    public NewMainPage(NewMainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}