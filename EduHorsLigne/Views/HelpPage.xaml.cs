using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class HelpPage : ContentPage
{
    public HelpPage(HelpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
