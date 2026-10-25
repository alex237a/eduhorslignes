namespace EduHorsLigne.Helpers;

/// <summary>Accès aux ressources de la charte (Colors.xaml) depuis le code.</summary>
public static class Res
{
    public static Color GetColor(string key) =>
        Application.Current!.Resources.TryGetValue(key, out var value) && value is Color c ? c : Colors.Magenta;

    /// <summary>Couleur claire ou sombre selon le thème courant.</summary>
    public static Color Themed(string lightKey, string darkKey) =>
        GetColor(Application.Current?.RequestedTheme == AppTheme.Dark ? darkKey : lightKey);

    public static Brush GetBrush(string key) => new SolidColorBrush(GetColor(key));
}
