using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class LessonDetailPage : ContentPage
{
    private readonly LessonDetailViewModel _vm;

    public LessonDetailPage(LessonDetailViewModel viewModel)
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
