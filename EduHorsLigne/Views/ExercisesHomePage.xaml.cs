using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class ExercisesHomePage : ContentPage
{
    public ExercisesHomePage(ExercisesHomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
