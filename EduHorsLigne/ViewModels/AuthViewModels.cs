using System.ComponentModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Api;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.ViewModels;

/// <summary>Règles de validation partagées (EF-81).</summary>
public static class AuthRules
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRx = new(@"^(\+237)?\d{9}$", RegexOptions.Compiled);

    public static bool IsValidIdentifier(string value)
    {
        var v = value.Replace(" ", string.Empty);
        return EmailRx.IsMatch(v) || PhoneRx.IsMatch(v);
    }

    /// <summary>Robustesse de 0 à 4.</summary>
    public static int Strength(string pwd)
    {
        if (pwd.Length == 0) return 0;
        var score = 0;
        if (pwd.Length >= 8) score++;
        if (pwd.Any(char.IsDigit) && pwd.Any(char.IsLetter)) score++;
        if (pwd.Any(char.IsUpper) && pwd.Any(char.IsLower)) score++;
        if (pwd.Any(c => !char.IsLetterOrDigit(c)) || pwd.Length >= 12) score++;
        return score;
    }

    /// <summary>Mot de passe accepté : 8 caractères minimum, une majuscule et un chiffre.</summary>
    public static bool IsAcceptable(string pwd) => pwd.Length >= 8 && pwd.Any(char.IsUpper) && pwd.Any(char.IsDigit);

    public static string StrengthLabel(int score) => score switch
    {
        0 => string.Empty,
        1 => Loc.Instance["pwd_weak"],
        2 or 3 => Loc.Instance["pwd_medium"],
        _ => Loc.Instance["pwd_strong"]
    };
}

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    public WelcomeViewModel(INavigationService nav) => _nav = nav;

    [RelayCommand] private Task RegisterAsync() => _nav.GoToAsync("register");
    [RelayCommand] private Task LoginAsync() => _nav.GoToAsync("//login");
    [RelayCommand] private Task GuestAsync() => _nav.GoToAsync("//main/home"); // TODO BACKEND : mode invité (EF-90)
}

public partial class LoginViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IAuthService _auth;

    public LoginViewModel(INavigationService nav, IAuthService auth)
    {
        _nav = nav;
        _auth = auth;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdentifierError), nameof(IdentifierTag), nameof(CanLogin))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _identifiant = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLogin))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty] private bool _rememberMe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLogin), nameof(ButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(ShowInfo))]
    private string? _errorMessage;

    [ObservableProperty] private string _attemptsText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool ShowInfo => !HasError;
    public string? IdentifierError =>
        Identifiant.Length > 0 && !AuthRules.IsValidIdentifier(Identifiant) ? Loc.Instance["err_identifier"] : null;
    public string? IdentifierTag => IdentifierError is null ? null : Loc.Instance["tag_invalid"];
    public bool CanLogin => !IsBusy && Identifiant.Length > 0 && Password.Length > 0 && IdentifierError is null;
    public string ButtonLabel => IsBusy ? Loc.Instance["login_busy"] : Loc.Instance["login_button"];

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        AttemptsText = string.Empty;
        IsBusy = true;
        var r = await _auth.LoginAsync(Identifiant.Trim(), Password);
        IsBusy = false;

        if (!r.Success)
        {
            if (r.AttemptsLeft is int left)
            {
                // Identifiants incorrects : message + nombre de tentatives restantes (EF-86).
                ErrorMessage = Loc.Instance["err_login_title"];
                AttemptsText = string.Format(Loc.Instance["err_login_attempts"], left);
            }
            else ErrorMessage = r.Message ?? Loc.Instance["err_login_title"]; // compte bloqué, hors ligne, etc.
            return;
        }

        Password = string.Empty;
        await _nav.GoToAsync("//main/home");
    }

    [RelayCommand] private Task GoRegisterAsync() => _nav.GoToAsync("register");
    [RelayCommand] private Task GoForgotAsync() => _nav.GoToAsync("forgot");
    [RelayCommand] private Task GuestAsync() => _nav.GoToAsync("//main/home");
}

public partial class RegisterViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;
    private readonly CatalogService _catalog;
    private List<ApiClasse> _allClasses = new();
    private List<ApiClasse> _shownClasses = new();
    private bool _busy;

    public RegisterViewModel(INavigationService nav, IAuthService auth, IDialogService dialogs, CatalogService catalog)
    {
        _nav = nav;
        _auth = auth;
        _dialogs = dialogs;
        _catalog = catalog;
        var loc = Loc.Instance;
        SousSystemes = new List<string> { loc["sub_fr"], loc["sub_en"] };
    }

    public IReadOnlyList<string> SousSystemes { get; }
    /// <summary>Classes du sous-système choisi, issues du catalogue du serveur.</summary>
    public ObservableCollection<string> Classes { get; } = new();

    [ObservableProperty] private string _nom = string.Empty;
    [ObservableProperty] private string _prenom = string.Empty;
    [ObservableProperty] private string _identifiant = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirm = string.Empty;
    [ObservableProperty] private int _sousSystemeIndex;
    [ObservableProperty] private int _classeIndex = -1;
    [ObservableProperty] private bool _acceptCgu;

    public bool NomValide => Nom.Length > 1;
    public bool PrenomValide => Prenom.Length > 1;
    public bool IdentifiantValide => Identifiant.Length > 0 && IdentifiantError is null;
    public bool ConfirmValide => Confirm.Length > 0 && ConfirmError is null;

    public string? IdentifiantError =>
        Identifiant.Length > 0 && !AuthRules.IsValidIdentifier(Identifiant) ? Loc.Instance["err_identifier_long"] : null;
    public string? IdentifiantTag => IdentifiantError is null ? null : Loc.Instance["tag_format"];
    public string? PasswordError =>
        Password.Length > 0 && !AuthRules.IsAcceptable(Password) ? Loc.Instance["err_password_weak"] : null;
    public string? PasswordTag => PasswordError is null ? null : Loc.Instance["tag_level"];
    public string? ConfirmError =>
        Confirm.Length > 0 && Confirm != Password ? Loc.Instance["err_confirm"] : null;
    public string? ConfirmTag => ConfirmError is null ? null : Loc.Instance["tag_not_same"];
    public string? CguError => !AcceptCgu && Confirm.Length > 0 ? Loc.Instance["err_cgu"] : null;
    public bool HasCguError => CguError is not null;

    public double Strength => AuthRules.Strength(Password) / 4.0;
    public string StrengthText => string.Format(Loc.Instance["pwd_strength"], (int)(Strength * 100));

    public int ErrorCount => new[] { IdentifiantError, PasswordError, ConfirmError, CguError }.Count(e => e is not null);
    public bool HasErrors => ErrorCount > 0;
    public string ErrorSummary => string.Format(Loc.Instance["err_summary"], ErrorCount);

    private ApiClasse? SelectedClasse() =>
        ClasseIndex >= 0 && ClasseIndex < _shownClasses.Count ? _shownClasses[ClasseIndex] : null;

    public bool CanSubmit =>
        NomValide && PrenomValide && IdentifiantValide && AuthRules.IsAcceptable(Password) && Confirm == Password && AcceptCgu
        && SelectedClasse() is not null;

    /// <summary>Charge les classes depuis le serveur (ou le dernier cache) ; appelé à l'ouverture de l'écran.</summary>
    public async Task LoadClassesAsync()
    {
        if (_allClasses.Count > 0) return;
        var r = await _catalog.GetClassesAsync();
        if (!r.Ok || r.Data is null)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"],
                Tx.T("Impossible de charger la liste des classes : l'inscription exige Internet.\n", "Could not load the class list: sign-up requires Internet.\n") + ApiSettings.BaseUrl);
            return;
        }
        _allClasses = r.Data;
        RebuildClasses();
    }

    partial void OnSousSystemeIndexChanged(int value) => RebuildClasses();

    private void RebuildClasses()
    {
        var wanted = SousSystemeIndex == 0 ? "Francophone" : "Anglophone";
        _shownClasses = _allClasses.Where(c => c.SousSysteme == wanted).ToList();
        Classes.Clear();
        foreach (var c in _shownClasses) Classes.Add(c.Nom);
        ClasseIndex = _shownClasses.Count > 0 ? 0 : -1;
        OnPropertyChanged(nameof(CanSubmit));
        RegisterCommand.NotifyCanExecuteChanged();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Nom) or nameof(Prenom) or nameof(Identifiant) or nameof(Password)
            or nameof(Confirm) or nameof(SousSystemeIndex) or nameof(ClasseIndex) or nameof(AcceptCgu))
        {
            foreach (var n in new[]
            {
                nameof(NomValide), nameof(PrenomValide), nameof(IdentifiantValide), nameof(ConfirmValide),
                nameof(IdentifiantError), nameof(IdentifiantTag), nameof(PasswordError), nameof(PasswordTag),
                nameof(ConfirmError), nameof(ConfirmTag), nameof(CguError), nameof(HasCguError), nameof(Strength),
                nameof(StrengthText), nameof(ErrorCount), nameof(HasErrors), nameof(ErrorSummary), nameof(CanSubmit)
            }) OnPropertyChanged(n);
            RegisterCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task RegisterAsync()
    {
        if (_busy || SelectedClasse() is not { } classe) return;
        _busy = true;
        try
        {
            var eleve = new Eleve
            {
                Nom = Nom.Trim(),
                Prenom = Prenom.Trim(),
                Langue = Loc.Instance.Language,
                SousSysteme = SousSystemeIndex == 0 ? SousSysteme.Francophone : SousSysteme.Anglophone
            };
            var r = await _auth.RegisterAsync(eleve, classe.Id, Identifiant.Trim(), Password);
            if (!r.Success)
            {
                await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], r.Message ?? Tx.T("Inscription impossible.", "Sign-up failed."));
                return;
            }
            await _nav.GoToAsync("//main/home");
        }
        finally { _busy = false; }
    }

    [RelayCommand] private Task GoLoginAsync() => _nav.GoToAsync("//login");
}

public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;
    private readonly SessionService _session;

    public ForgotPasswordViewModel(INavigationService nav, IAuthService auth, IDialogService dialogs, SessionService session)
    {
        _nav = nav; _auth = auth; _dialogs = dialogs; _session = session;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _identifiant = string.Empty;

    public bool CanSend => AuthRules.IsValidIdentifier(Identifiant);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var r = await _auth.SendResetCodeAsync(Identifiant.Trim());
        if (!r.Success)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], r.Message ?? "");
            return;
        }
        _session.PendingResetIdentifiant = Identifiant.Trim();
        // Démo : le SMS / e-mail est simulé, le serveur renvoie le code en mode développement.
        if (!string.IsNullOrEmpty(r.Message))
            await _dialogs.ShowMessageAsync(Tx.T("Code de démonstration", "Demo code"),
                Tx.T($"Votre code de vérification : {r.Message}", $"Your verification code: {r.Message}"));
        await _nav.GoToAsync("otp");
    }
}

/// <summary>Écran « Vérification du code » : code à 6 chiffres + nouveau mot de passe.</summary>
public partial class OtpViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly IAuthService _auth;
    private readonly SessionService _session;

    public OtpViewModel(INavigationService nav, IDialogService dialogs, IAuthService auth, SessionService session)
    {
        _nav = nav; _dialogs = dialogs; _auth = auth; _session = session;
    }

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirm = string.Empty;

    public string? PasswordError => Password.Length > 0 && Password.Length < 8 ? Loc.Instance["err_password_short"] : null;
    public string? ConfirmError => Confirm.Length > 0 && Confirm != Password ? Loc.Instance["err_confirm"] : null;
    public bool CanSubmit => Code.Length == 6 && Password.Length >= 8 && Confirm == Password;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Code) or nameof(Password) or nameof(Confirm))
        {
            OnPropertyChanged(nameof(PasswordError));
            OnPropertyChanged(nameof(ConfirmError));
            OnPropertyChanged(nameof(CanSubmit));
            SubmitCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        var id = _session.PendingResetIdentifiant;
        if (string.IsNullOrEmpty(id))
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Recommencez la procédure.", "Please start again."));
            await _nav.GoToAsync("//login");
            return;
        }
        var r = await _auth.SetNewPasswordAsync(id, Code.Trim(), Password);
        if (!r.Success)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], r.Message ?? "");
            return;
        }
        _session.PendingResetIdentifiant = null;
        await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Mot de passe modifié. Connectez-vous.", "Password changed. Please sign in."));
        await _nav.GoToAsync("//login");
    }

    [RelayCommand]
    private async Task ResendAsync()
    {
        var id = _session.PendingResetIdentifiant;
        if (string.IsNullOrEmpty(id)) return;
        var r = await _auth.SendResetCodeAsync(id);
        var text = !r.Success ? r.Message ?? "" : string.IsNullOrEmpty(r.Message)
            ? Tx.T("Un nouveau code a été envoyé.", "A new code has been sent.")
            : Tx.T($"Nouveau code de démonstration : {r.Message}", $"New demo code: {r.Message}");
        await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], text);
    }
}
