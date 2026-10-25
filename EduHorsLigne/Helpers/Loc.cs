using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace EduHorsLigne.Helpers;

/// <summary>
/// Gestionnaire de localisation FR/EN. Les clés viennent de Resources/Strings/Strings(.en).resx.
/// Le changement de langue est appliqué à chaud : tous les bindings « [clé] » sont rafraîchis.
/// Une clé peut forcer une langue : « cle|en » (utile pour la galerie des états).
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private readonly ResourceManager _resources =
        new("EduHorsLigne.Resources.Strings.Strings", typeof(Loc).Assembly);
    private CultureInfo _culture = new("fr");

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
    {
        get
        {
            var culture = _culture;
            var k = key;
            var sep = key.IndexOf('|');
            if (sep > 0)
            {
                culture = new CultureInfo(key[(sep + 1)..]);
                k = key[..sep];
            }
            return _resources.GetString(k, culture) ?? $"[{k}]";
        }
    }

    /// <summary>Code de langue courant : « fr » ou « en ».</summary>
    public string Language => _culture.TwoLetterISOLanguageName;

    public void SetLanguage(string code)
    {
        if (code == Language) return;
        _culture = new CultureInfo(code);
        CultureInfo.CurrentUICulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }
}
