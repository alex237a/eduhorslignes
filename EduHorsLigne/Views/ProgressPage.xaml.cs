using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class ProgressPage : ContentPage
{
    private readonly ProgressViewModel _vm;

    public ProgressPage(ProgressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
