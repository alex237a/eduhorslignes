using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnChipClicked(object? sender, EventArgs e)
    {
        if (sender is Button button) _vm.OpenSubjectCommand.Execute(button.ClassId);
    }
}
