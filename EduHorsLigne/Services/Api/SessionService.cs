using System.Text.Json;
using EduHorsLigne.Models;

namespace EduHorsLigne.Services.Api;

/// <summary>
/// Session de l'élève, conservée sur l'appareil : après la première connexion (qui exige Internet, EF-84),
/// l'application reste utilisable hors ligne sans reconnexion tant que le jeton n'a pas expiré.
/// </summary>
public class SessionService
{
    private const string TokenKey = "session_token";
    private const string UserKey = "session_user";
    private const string ExpKey = "session_expires";

    public string? Token { get; private set; }
    public ApiUser? User { get; private set; }
    public DateTime? ExpiresUtc { get; private set; }

    /// <summary>Identifiant saisi à l'écran « Mot de passe oublié », repris par l'écran du code.</summary>
    public string? PendingResetIdentifiant { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token) && User != null && (ExpiresUtc is null || ExpiresUtc > DateTime.UtcNow);

    public SessionService() => Load();

    public void Save(ApiAuth auth)
    {
        Token = auth.Token;
        User = auth.Utilisateur;
        ExpiresUtc = auth.ExpiresUtc;
        Preferences.Default.Set(TokenKey, auth.Token);
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(auth.Utilisateur, JsonDefaults.Options));
        Preferences.Default.Set(ExpKey, auth.ExpiresUtc.ToString("O"));
    }

    public void Update(ApiUser user)
    {
        User = user;
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(user, JsonDefaults.Options));
    }

    public void Clear()
    {
        Token = null; User = null; ExpiresUtc = null;
        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserKey);
        Preferences.Default.Remove(ExpKey);
    }

    private void Load()
    {
        try
        {
            Token = Preferences.Default.Get<string?>(TokenKey, null);
            var u = Preferences.Default.Get<string?>(UserKey, null);
            User = u is null ? null : JsonSerializer.Deserialize<ApiUser>(u, JsonDefaults.Options);
            var e = Preferences.Default.Get<string?>(ExpKey, null);
            ExpiresUtc = DateTime.TryParse(e, null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;
        }
        catch { Token = null; User = null; ExpiresUtc = null; }
    }

    /// <summary>Profil de l'élève au format utilisé par les écrans.</summary>
    public Eleve CurrentEleve() => User is null
        ? new Eleve { Prenom = Helpers.Tx.T("Invité", "Guest"), Nom = "", Classe = "" }
        : new Eleve
        {
            Id = User.Id,
            Prenom = User.Prenom,
            Nom = User.Nom,
            Classe = User.ClasseNom ?? "",
            Langue = User.Langue,
            SousSysteme = User.SousSysteme == "Anglophone" ? SousSysteme.Anglophone : SousSysteme.Francophone
        };
}
