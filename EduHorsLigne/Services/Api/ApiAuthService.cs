using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.Services.Api;

/// <summary>Authentification via l'API. L'inscription et la première connexion exigent Internet (EF-84).</summary>
public class ApiAuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly SessionService _session;
    private readonly LocalStateService _local;
    private readonly LessonStorage _storage;
    private readonly CatalogService _catalog;

    public ApiAuthService(ApiClient api, SessionService session, LocalStateService local, LessonStorage storage, CatalogService catalog)
    {
        _api = api; _session = session; _local = local; _storage = storage; _catalog = catalog;
    }

    public async Task<ServiceResult<Eleve>> LoginAsync(string identifiant, string motDePasse, CancellationToken ct = default)
    {
        var r = await _api.PostAsync<ApiAuth>("/api/auth/login", new { identifiant, motDePasse }, false, ct);
        return ToEleveResult(r);
    }

    public async Task<ServiceResult<Eleve>> RegisterAsync(Eleve profil, string classeId, string identifiant, string motDePasse, CancellationToken ct = default)
    {
        var r = await _api.PostAsync<ApiAuth>("/api/auth/register", new
        {
            nom = profil.Nom,
            prenom = profil.Prenom,
            identifiant,
            motDePasse,
            confirmationMotDePasse = motDePasse,
            classeId,
            langue = profil.Langue,
            accepteConditions = true
        }, false, ct);
        return ToEleveResult(r);
    }

    public async Task<ServiceResult<bool>> SendResetCodeAsync(string identifiant, CancellationToken ct = default)
    {
        var r = await _api.PostAsync<ApiResetResponse>("/api/auth/forgot-password", new { identifiant }, false, ct);
        // En développement le serveur renvoie le code (SMS/e-mail simulé) : on le transmet pour la démo.
        return r.Ok ? new(true, true, r.Data?.DevCode) : new(false, false, Describe(r), null, r.Offline);
    }

    public async Task<ServiceResult<bool>> VerifyCodeAsync(string identifiant, string code, CancellationToken ct = default)
    {
        var r = await _api.PostAsync<object>("/api/auth/verify-code", new { identifiant, code }, false, ct);
        return r.Ok ? new(true, true) : new(false, false, Describe(r), null, r.Offline);
    }

    public async Task<ServiceResult<bool>> SetNewPasswordAsync(string identifiant, string code, string nouveauMotDePasse, CancellationToken ct = default)
    {
        var r = await _api.PostAsync<object>("/api/auth/reset-password", new { identifiant, code, nouveauMotDePasse }, false, ct);
        return r.Ok ? new(true, true) : new(false, false, Describe(r), null, r.Offline);
    }

    /// <summary>Déconnexion ; EF-87 : l'élève choisit de conserver ou d'effacer les données locales.</summary>
    public Task LogoutAsync(bool effacerDonneesLocales, CancellationToken ct = default)
    {
        _session.Clear();
        if (effacerDonneesLocales) WipeLocalData();
        return Task.CompletedTask;
    }

    public async Task<ServiceResult<bool>> DeleteAccountAsync(CancellationToken ct = default)
    {
        var r = await _api.DeleteAsync<object>("/api/me", true, ct);
        if (!r.Ok) return new(false, false, Describe(r), null, r.Offline);
        _session.Clear();
        WipeLocalData();
        return new(true, true);
    }

    private void WipeLocalData()
    {
        _local.Clear();
        _storage.DeleteAll();
        _catalog.ClearCaches();
    }

    private ServiceResult<Eleve> ToEleveResult(ApiResult<ApiAuth> r)
    {
        if (r.Ok && r.Data != null)
        {
            _session.Save(r.Data);
            return new(true, _session.CurrentEleve());
        }
        return new(false, null, Describe(r), r.Error?.TentativesRestantes, r.Offline);
    }

    /// <summary>Message lisible : message général, complété par les erreurs de champs éventuelles.</summary>
    private static string Describe<T>(ApiResult<T> r)
    {
        if (r.Offline)
            return Tx.T("Cette action exige Internet. Vérifiez votre connexion puis réessayez.", "This action requires Internet. Check your connection and retry.")
                   + $"\n({ApiSettings.BaseUrl})";
        var e = r.Error;
        if (e == null) return Tx.T("Erreur inconnue.", "Unknown error.");
        return e.Fields is { Count: > 0 } ? string.Join("\n", e.Fields.Values) : e.Message;
    }
}
