namespace EduHorsLigne.Helpers;

/// <summary>
/// Propriétés attachées pour les boutons dont l'action métier n'est pas encore implémentée.
/// Demo.ComingSoon="True"          : affiche « Fonction à venir ».
/// Demo.Confirm="cle_message"      : affiche une confirmation d'avertissement (texte localisé).
/// </summary>
public static class Demo
{
    public static readonly BindableProperty ComingSoonProperty =
        BindableProperty.CreateAttached("ComingSoon", typeof(bool), typeof(Demo), false, propertyChanged: OnComingSoonChanged);

    public static bool GetComingSoon(BindableObject b) => (bool)b.GetValue(ComingSoonProperty);
    public static void SetComingSoon(BindableObject b, bool value) => b.SetValue(ComingSoonProperty, value);

    private static void OnComingSoonChanged(BindableObject b, object? oldValue, object? newValue)
    {
        if (b is not Button button) return;
        button.Clicked -= OnComingSoonClicked;
        if (newValue is true) button.Clicked += OnComingSoonClicked;
    }

    private static async void OnComingSoonClicked(object? sender, EventArgs e) => await Dialogs.ComingSoonAsync();

    public static readonly BindableProperty ConfirmProperty =
        BindableProperty.CreateAttached("Confirm", typeof(string), typeof(Demo), null, propertyChanged: OnConfirmChanged);

    public static string? GetConfirm(BindableObject b) => (string?)b.GetValue(ConfirmProperty);
    public static void SetConfirm(BindableObject b, string? value) => b.SetValue(ConfirmProperty, value);

    private static void OnConfirmChanged(BindableObject b, object? oldValue, object? newValue)
    {
        if (b is not Button button) return;
        button.Clicked -= OnConfirmClicked;
        if (newValue is string) button.Clicked += OnConfirmClicked;
    }

    private static async void OnConfirmClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || GetConfirm(button) is not { } key) return;
        var loc = Loc.Instance;
        await Dialogs.ConfirmAsync(loc["warning"], loc[key], loc["confirm"], loc["cancel"]);
        // TODO BACKEND : exécuter l'action si l'élève confirme.
    }
}
