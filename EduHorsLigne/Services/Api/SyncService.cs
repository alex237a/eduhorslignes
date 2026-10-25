using EduHorsLigne.Models;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.Services.Api;

/// <summary>
/// Synchronisation des favoris et de la progression avec le serveur (dernière modification gagnante, EF-63) et
/// détection des mises à jour de leçons (EF-23). Silencieuse et sans effet bloquant : sans réseau, elle échoue proprement.
/// </summary>
public class SyncService : ISyncService
{
    private readonly ApiClient _api;
    private readonly SessionService _session;
    private readonly LocalStateService _local;
    private int _running;

    public SyncService(ApiClient api, SessionService session, LocalStateService local)
    {
        _api = api; _session = session; _local = local;
    }

    public async Task<ServiceResult<bool>> SyncAsync(CancellationToken ct = default)
    {
        if (!_session.IsLoggedIn) return new(false, false, "non connecté");
        if (Interlocked.Exchange(ref _running, 1) == 1) return new(true, true);
        try
        {
            var since = Preferences.Default.Get<string?>("sync_since", null);

            var favoris = _local.All().Where(l => l.FavoriUpdatedUtc != null)
                .Select(l => new ApiSyncFavori(l.Id, null, l.FavoriUpdatedUtc!.Value, !l.Favori)).ToList();
            var progressions = _local.All().Where(l => l.Statut is LessonStatus.InProgress or LessonStatus.Completed)
                .Select(l => new ApiSyncProgression(l.Id, l.Statut == LessonStatus.Completed ? "Terminee" : "EnCours",
                    Math.Clamp(l.Avancement, 0, 1), 0, l.Avancement, l.UpdatedUtc)).ToList();

            var r = await _api.PostAsync<ApiSyncResponse>("/api/sync",
                new { sinceUtc = since, favoris, progressions }, true, ct);
            if (!r.Ok || r.Data == null) return new(false, false, r.Error?.Message, null, r.Offline);

            // Application des changements venus d'un autre appareil (le plus récent gagne).
            foreach (var f in r.Data.Favoris ?? new())
            {
                var cur = _local.Get(f.LeconId);
                if (cur?.FavoriUpdatedUtc is { } t && t >= f.UpdatedUtc) continue;
                _local.Update(f.LeconId, l => { l.Favori = !f.IsDeleted; l.FavoriUpdatedUtc = f.UpdatedUtc; });
            }
            foreach (var p in r.Data.Progressions ?? new())
            {
                var cur = _local.Get(p.LeconId);
                if (cur != null && cur.UpdatedUtc >= p.UpdatedUtc) continue;
                _local.Update(p.LeconId, l =>
                {
                    l.Statut = p.Statut == "Terminee" ? LessonStatus.Completed : p.Statut == "EnCours" ? LessonStatus.InProgress : l.Statut;
                    l.Avancement = p.PourcentageLecture;
                    l.UpdatedUtc = p.UpdatedUtc;
                });
            }

            Preferences.Default.Set("sync_since", r.Data.ServerTimeUtc.ToString("O"));
            return new(true, true);
        }
        catch (Exception ex)
        {
            return new(false, false, ex.Message);
        }
        finally { Interlocked.Exchange(ref _running, 0); }
    }

    /// <summary>Lance la synchronisation en arrière-plan sans attendre.</summary>
    public void Trigger() => _ = Task.Run(() => SyncAsync());
}
