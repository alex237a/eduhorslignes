using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class QuizPage : ContentPage
{
    public QuizPage(QuizViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
