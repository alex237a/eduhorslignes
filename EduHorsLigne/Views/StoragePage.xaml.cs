using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views;

public partial class StoragePage : ContentPage
{
    public StoragePage(StorageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
