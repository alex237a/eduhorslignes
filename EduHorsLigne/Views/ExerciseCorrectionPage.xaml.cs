using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class ExerciseCorrectionPage : ContentPage
{
    public ExerciseCorrectionPage(ExerciseCorrectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
