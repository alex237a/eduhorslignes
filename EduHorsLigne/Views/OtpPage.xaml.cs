using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class OtpPage : ContentPage
{
    public OtpPage(OtpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
