using EduHorsLigne.Helpers;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.Services;

public class NavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}

public class DialogService : IDialogService
{
    public Task ShowComingSoonAsync() => Dialogs.ComingSoonAsync();
    public Task ShowMessageAsync(string title, string message) => Dialogs.MessageAsync(title, message);
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        Dialogs.ConfirmAsync(title, message, accept, cancel);
}
