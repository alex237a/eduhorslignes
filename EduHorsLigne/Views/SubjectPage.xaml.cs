using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class SubjectPage : ContentPage
{
    private readonly SubjectViewModel _vm;

    public SubjectPage(SubjectViewModel viewModel)
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
