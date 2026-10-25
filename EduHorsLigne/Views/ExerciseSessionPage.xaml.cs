using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class ExerciseSessionPage : ContentPage
{
    public ExerciseSessionPage(ExerciseSessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
