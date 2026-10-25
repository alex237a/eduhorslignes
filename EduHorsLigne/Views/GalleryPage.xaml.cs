using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class GalleryPage : ContentPage
{
    public GalleryPage(GalleryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
