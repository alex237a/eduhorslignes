using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Api;
using EduHorsLigne.Services.Interfaces;
using EduHorsLigne.Services.Mock;

namespace EduHorsLigne.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly IAuthService _auth;
    private readonly SessionService _session;

    public ProfileViewModel(INavigationService nav, IDialogService dialogs, IAuthService auth, SessionService session)
    {
        _nav = nav;
        _dialogs = dialogs;
        _auth = auth;
        _session = session;
        _eleve = session.CurrentEleve();
    }

    [ObservableProperty] private Eleve _eleve;

    public void Refresh() => Eleve = _session.CurrentEleve();

    [RelayCommand] private Task OpenSettingsAsync() => _nav.GoToAsync("settings");
    [RelayCommand] private Task OpenStorageAsync() => _nav.GoToAsync("storage");
    [RelayCommand] private Task OpenHelpAsync() => _nav.GoToAsync("help");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var loc = Loc.Instance;
        // EF-87 : l'élève choisit de conserver ou d'effacer les données locales.
        var keep = await _dialogs.ConfirmAsync(loc["logout_title"], loc["logout_message"], loc["logout_keep"], loc["logout_erase"]);
        await _auth.LogoutAsync(effacerDonneesLocales: !keep);
        await _nav.GoToAsync("//login");
    }
}

public partial class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public SettingsViewModel(INavigationService nav)
    {
        _nav = nav;
        _isFrench = Loc.Instance.Language == "fr";
        _isEnglish = !_isFrench;
        _isDark = Application.Current?.UserAppTheme == AppTheme.Dark;
        _stateIndex = (int)DemoState.Current;
        var loc = Loc.Instance;
        StateOptions = new List<string> { loc["state_content"], loc["state_loading"], loc["state_empty"], loc["state_error"] };
    }

    public IReadOnlyList<string> StateOptions { get; }

    [ObservableProperty] private bool _isFrench;
    [ObservableProperty] private bool _isEnglish;
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private double _textScale = 1.0;
    [ObservableProperty] private bool _wifiOnly = true;
    [ObservableProperty] private bool _notifications = true;
    [ObservableProperty] private bool _pinEnabled;
    [ObservableProperty] private int _stateIndex;

    public double PreviewFontSize => 16 * TextScale;

    partial void OnIsFrenchChanged(bool value) { if (value) Loc.Instance.SetLanguage("fr"); }  // TODO BACKEND : IParametresService
    partial void OnIsEnglishChanged(bool value) { if (value) Loc.Instance.SetLanguage("en"); }

    partial void OnIsDarkChanged(bool value)
    {
        if (Application.Current is { } app) app.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    partial void OnTextScaleChanged(double value) => OnPropertyChanged(nameof(PreviewFontSize));
    partial void OnStateIndexChanged(int value) => DemoState.Set((PageState)Math.Clamp(value, 0, 3));

    [RelayCommand] private Task OpenHelpAsync() => _nav.GoToAsync("help");
    [RelayCommand] private Task OpenGalleryAsync() => _nav.GoToAsync("gallery");
}

public partial class HelpViewModel : BaseViewModel
{
    public HelpViewModel(MockDataProvider data) => Faq = data.Faq;
    public IReadOnlyList<FaqItem> Faq { get; }
}

public partial class GalleryViewModel : BaseViewModel
{
    public GalleryViewModel(MockDataProvider data) => Lecons = data.Lecons;

    public IReadOnlyList<Lecon> Lecons { get; }
    public string SampleText => "Exemple de texte";
    public double SampleProgress => 0.45;
}
