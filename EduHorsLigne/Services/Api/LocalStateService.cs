using System.Text.Json;
using EduHorsLigne.Models;

namespace EduHorsLigne.Services.Api;

/// <summary>État local d'une leçon : téléchargement, progression, favori. Survit à la suppression des fichiers (RG3, EF-20).</summary>
public class LocalLesson
{
    public string Id { get; set; } = "";
    public string Titre { get; set; } = "";
    public string MatiereNom { get; set; } = "";
    public bool Downloaded { get; set; }
    public int Version { get; set; }
    public string? Checksum { get; set; }
    public long SizeBytes { get; set; }
    public DateTime? DownloadedUtc { get; set; }
    public LessonStatus Statut { get; set; } = LessonStatus.Downloaded;
    public double Avancement { get; set; }
    public bool Favori { get; set; }
    public DateTime? FavoriUpdatedUtc { get; set; }
    /// <summary>Horloge de la dernière modification de progression (sert à la synchronisation).</summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Base locale simple (fichier JSON écrit de façon atomique) : fonctionne entièrement hors ligne.</summary>
public class LocalStateService
{
    private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "local-state.json");
    private readonly object _lock = new();
    private Dictionary<string, LocalLesson> _items = new();

    public LocalStateService() => Load();

    public string? LastLessonId
    {
        get => Preferences.Default.Get<string?>("last_lesson_id", null);
        set { if (value is null) Preferences.Default.Remove("last_lesson_id"); else Preferences.Default.Set("last_lesson_id", value); }
    }

    public LocalLesson? Get(string id) { lock (_lock) return _items.GetValueOrDefault(id); }

    public IReadOnlyList<LocalLesson> All() { lock (_lock) return _items.Values.ToList(); }

    /// <summary>Modifie (ou crée) l'état d'une leçon puis enregistre.</summary>
    public LocalLesson Update(string id, Action<LocalLesson> change)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(id, out var l)) { l = new LocalLesson { Id = id }; _items[id] = l; }
            change(l);
            Save();
            return l;
        }
    }

    public long TotalDownloadedBytes() { lock (_lock) return _items.Values.Where(l => l.Downloaded).Sum(l => l.SizeBytes); }

    public void Clear()
    {
        lock (_lock) { _items = new(); Save(); }
        LastLessonId = null;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                _items = JsonSerializer.Deserialize<Dictionary<string, LocalLesson>>(File.ReadAllText(FilePath), JsonDefaults.Options) ?? new();
        }
        catch { _items = new(); }
    }

    private void Save()
    {
        try
        {
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_items, JsonDefaults.Options));
            File.Move(tmp, FilePath, overwrite: true); // écriture atomique : pas de perte en cas d'arrêt brutal
        }
        catch { /* l'état en mémoire reste valable */ }
    }
}
