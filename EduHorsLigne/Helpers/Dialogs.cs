#pragma warning disable CS0618 // DisplayAlert : API utilisée volontairement pour rester compatible
namespace EduHorsLigne.Helpers;

/// <summary>Boîtes de dialogue visuelles utilisées par la maquette interactive.</summary>
public static class Dialogs
{
    public static Task ComingSoonAsync() =>
        Shell.Current.DisplayAlert(Loc.Instance["app_name"], Loc.Instance["coming_soon"], Loc.Instance["ok"]);

    public static Task MessageAsync(string title, string message) =>
        Shell.Current.DisplayAlert(title, message, Loc.Instance["ok"]);

    public static Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        Shell.Current.DisplayAlert(title, message, accept, cancel);
}
