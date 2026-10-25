using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class ExerciseResultPage : ContentPage
{
    public ExerciseResultPage(ExerciseResultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
