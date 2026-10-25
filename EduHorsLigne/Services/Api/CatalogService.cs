using System.Text.Json;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.Services.Api;

/// <summary>
/// Catalogue : téléchargé depuis l'API puis mis en cache sur l'appareil, de sorte que l'élève voie, hors ligne,
/// toutes les leçons disponibles même non téléchargées (EF-14).
/// </summary>
public class CatalogService : ICatalogueService
{
    private readonly ApiClient _api;
    private readonly LocalStateService _local;
    private ApiCatalog? _catalog;
    private string? _catalogClasse;
    private DateTime _loadedUtc = DateTime.MinValue;

    public CatalogService(ApiClient api, LocalStateService local)
    {
        _api = api;
        _local = local;
    }

    private static string CachePath(string classeId) => Path.Combine(FileSystem.AppDataDirectory, $"catalog-{classeId}.json");
    private static string ClassesPath => Path.Combine(FileSystem.AppDataDirectory, "classes.json");

    // ---------- Chargement ----------

    /// <summary>Charge le catalogue d'une classe : réseau si possible (au plus toutes les 2 minutes), sinon cache disque.</summary>
    public async Task<(ApiCatalog? Catalog, bool FromCache, bool Offline)> LoadCatalogAsync(string? classeId, bool force = false, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(classeId)) return (null, false, false);

        if (!force && _catalog != null && _catalogClasse == classeId && DateTime.UtcNow - _loadedUtc < TimeSpan.FromMinutes(2))
            return (_catalog, false, false);

        var r = await _api.GetAsync<ApiCatalog>($"/api/catalog?classeId={Uri.EscapeDataString(classeId)}", false, ct);
        if (r is { Ok: true, Data: not null })
        {
            _catalog = r.Data; _catalogClasse = classeId; _loadedUtc = DateTime.UtcNow;
            TryWrite(CachePath(classeId), r.Data);
            return (r.Data, false, false);
        }

        if (_catalog != null && _catalogClasse == classeId) return (_catalog, true, r.Offline);
        var cached = TryRead<ApiCatalog>(CachePath(classeId));
        if (cached != null)
        {
            _catalog = cached; _catalogClasse = classeId; _loadedUtc = DateTime.UtcNow.AddMinutes(-1);
            return (cached, true, r.Offline);
        }
        return (null, false, r.Offline);
    }

    /// <summary>Classes proposées à l'inscription (réseau, sinon dernier cache).</summary>
    public async Task<ApiResult<List<ApiClasse>>> GetClassesAsync(CancellationToken ct = default)
    {
        var r = await _api.GetAsync<List<ApiClasse>>("/api/classes", false, ct);
        if (r.Ok && r.Data != null) { TryWrite(ClassesPath, r.Data); return r; }
        var cached = TryRead<List<ApiClasse>>(ClassesPath);
        return cached != null ? new ApiResult<List<ApiClasse>>(true, cached, null, r.Offline, 200) : r;
    }

    public void ClearCaches()
    {
        _catalog = null; _catalogClasse = null;
        foreach (var f in Directory.GetFiles(FileSystem.AppDataDirectory, "catalog-*.json")) File.Delete(f);
        if (File.Exists(ClassesPath)) File.Delete(ClassesPath);
    }

    // ---------- Navigation dans l'arbre ----------

    private IEnumerable<ApiMatiereTree> Matieres() => _catalog?.Classes.SelectMany(c => c.Matieres) ?? Enumerable.Empty<ApiMatiereTree>();

    public IReadOnlyList<ApiMatiereTree> GetMatiereTrees() => Matieres().ToList();
    public ApiMatiereTree? GetMatiereTree(string? id) => Matieres().FirstOrDefault(m => m.Matiere.Id == id);
    public string? ClasseNom => _catalog?.Classes.FirstOrDefault()?.Classe.Nom;

    public ApiLecon? FindLecon(string? id) =>
        id is null ? null : Matieres().SelectMany(m => m.Chapitres).SelectMany(c => c.Lecons).FirstOrDefault(l => l.Id == id);

    public IReadOnlyList<ApiLecon> AllLecons() => Matieres().SelectMany(m => m.Chapitres).SelectMany(c => c.Lecons).ToList();

    /// <summary>Leçons précédente et suivante dans la même matière (ordre des chapitres puis des leçons).</summary>
    public (string? Previous, string? Next) GetSiblings(string leconId)
    {
        foreach (var m in Matieres())
        {
            var ordered = m.Chapitres.SelectMany(c => c.Lecons).ToList();
            var i = ordered.FindIndex(l => l.Id == leconId);
            if (i >= 0) return (i > 0 ? ordered[i - 1].Id : null, i < ordered.Count - 1 ? ordered[i + 1].Id : null);
        }
        return (null, null);
    }

    // ---------- Conversion vers les modèles des écrans ----------

    public Matiere ToMatiere(ApiMatiereTree t)
    {
        var all = t.Chapitres.SelectMany(c => c.Lecons).ToList();
        var done = all.Count(l => _local.Get(l.Id) is { Statut: LessonStatus.Completed });
        var downloaded = all.Count(l => _local.Get(l.Id) is { Downloaded: true });
        return new Matiere
        {
            Id = t.Matiere.Id,
            Nom = t.Matiere.Nom,
            Initiale = t.Matiere.Initiale,
            Icone = t.Matiere.Icone,
            Ton = t.Matiere.Ton,
            NbLecons = all.Count,
            NbTerminees = done,
            TailleMo = Math.Round(t.Matiere.TailleOctets / 1048576.0, 2),
            Progression = all.Count == 0 ? 0 : (double)done / all.Count,
            HorsLigneVide = downloaded == 0,
            Meta = Helpers.Tx.T($"{t.Chapitres.Count} chap. · {downloaded} téléchargée(s)", $"{t.Chapitres.Count} ch. · {downloaded} downloaded")
        };
    }

    public Lecon ToLecon(ApiLecon l)
    {
        var loc = _local.Get(l.Id);
        var statut = LessonStatus.NotDownloaded;
        double avancement = 0;
        if (loc is { Downloaded: true })
        {
            if (loc.Version < l.Version) statut = LessonStatus.UpdateAvailable;
            else
            {
                statut = loc.Statut is LessonStatus.InProgress or LessonStatus.Completed ? loc.Statut : LessonStatus.Downloaded;
                avancement = loc.Avancement;
            }
        }
        return new Lecon
        {
            Id = l.Id,
            Version = l.Version,
            ChapitreId = l.ChapitreId,
            Titre = l.Titre,
            ChapitreTitre = l.ChapitreTitre,
            MatiereNom = l.MatiereNom,
            DureeMin = l.DureeMin,
            TailleMo = l.TailleOctets / 1048576.0,
            Statut = statut,
            Avancement = avancement,
            EstFavori = loc?.Favori ?? false,
            UpdatedUtc = l.DateMiseAJour
        };
    }

    public IReadOnlyList<LeconGroup> GetGroups(string? matiereId, Func<Lecon, bool>? filter = null)
    {
        var tree = GetMatiereTree(matiereId);
        if (tree == null) return Array.Empty<LeconGroup>();
        var groups = new List<LeconGroup>();
        var index = 0;
        foreach (var ch in tree.Chapitres)
        {
            var lecons = ch.Lecons.Select(ToLecon).Where(l => filter?.Invoke(l) ?? true).ToList();
            if (lecons.Count > 0) groups.Add(new LeconGroup(ch.Chapitre.Titre, lecons, ch.Lecons.Count, index == 0));
            index++;
        }
        return groups;
    }

    // ---------- ICatalogueService ----------

    public async Task<IReadOnlyList<Matiere>> GetMatieresAsync(string classeId, CancellationToken ct = default)
    {
        await LoadCatalogAsync(classeId, false, ct);
        return GetMatiereTrees().Select(ToMatiere).ToList();
    }

    public Task<IReadOnlyList<Chapitre>> GetChapitresAsync(string matiereId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Chapitre>>(GetMatiereTree(matiereId)?.Chapitres
            .Select(c => new Chapitre { Id = c.Chapitre.Id, MatiereId = matiereId, Titre = c.Chapitre.Titre, Ordre = c.Chapitre.Ordre }).ToList()
            ?? new List<Chapitre>());

    public Task<IReadOnlyList<Lecon>> GetLeconsAsync(string chapitreId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Lecon>>(Matieres().SelectMany(m => m.Chapitres).FirstOrDefault(c => c.Chapitre.Id == chapitreId)?
            .Lecons.Select(ToLecon).ToList() ?? new List<Lecon>());

    // ---------- Cache disque ----------

    private static void TryWrite<T>(string path, T value)
    {
        try { File.WriteAllText(path, JsonSerializer.Serialize(value, JsonDefaults.Options)); } catch { /* cache facultatif */ }
    }

    private static T? TryRead<T>(string path)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonDefaults.Options) : default; }
        catch { return default; }
    }
}
