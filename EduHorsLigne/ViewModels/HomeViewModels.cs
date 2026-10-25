using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Api;
using EduHorsLigne.Services.Interfaces;
using EduHorsLigne.Services.Mock;

namespace EduHorsLigne.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly SessionService _session;
    private readonly CatalogService _catalog;
    private readonly LocalStateService _local;
    private readonly SyncService _sync;

    public HomeViewModel(INavigationService nav, SessionService session, CatalogService catalog, LocalStateService local, SyncService sync)
    {
        _nav = nav; _session = session; _catalog = catalog; _local = local; _sync = sync;
        _eleve = session.CurrentEleve();
    }

    [ObservableProperty] private Eleve _eleve;
    [ObservableProperty] private string _continueTitle = string.Empty;
    [ObservableProperty] private string _continueContext = string.Empty;
    [ObservableProperty] private double _continueProgress;
    [ObservableProperty] private string _globalPercent = "0 %";
    [ObservableProperty] private string _storageText = "0 Mo / 1 Go";
    [ObservableProperty] private double _storageFraction;

    public ObservableCollection<Matiere> Matieres { get; } = new();
    public ObservableCollection<Matiere> Recommandees { get; } = new();

    public int Streak => 0;
    public string WeekTime => "—";

    /// <summary>L'accueil dessine lui-même son état vide (maquette « Aucune leçon pour l'instant »).</summary>
    public bool IsEmptyHome => State == PageState.Empty;
    public bool IsFilledHome => State != PageState.Empty;

    [ObservableProperty] private Matiere? _selectedMatiere;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(State))
        {
            OnPropertyChanged(nameof(IsEmptyHome));
            OnPropertyChanged(nameof(IsFilledHome));
        }
    }

    /// <summary>Charge le catalogue de la classe de l'élève (réseau, sinon cache) et l'état local.</summary>
    public async Task LoadAsync()
    {
        Eleve = _session.CurrentEleve();
        var classeId = _session.User?.ClasseId;
        if (classeId is null) { Matieres.Clear(); Recommandees.Clear(); State = PageState.Empty; return; }

        if (Matieres.Count == 0) State = PageState.Loading;
        var (catalog, _, _) = await _catalog.LoadCatalogAsync(classeId);

        Matieres.Clear();
        Recommandees.Clear();
        if (catalog is null) { State = PageState.Error; return; }

        foreach (var t in _catalog.GetMatiereTrees()) Matieres.Add(_catalog.ToMatiere(t));
        foreach (var m in Matieres.Take(3)) Recommandees.Add(m);

        var all = _catalog.AllLecons();
        var done = all.Count(l => _local.Get(l.Id) is { Statut: LessonStatus.Completed });
        GlobalPercent = all.Count == 0 ? "0 %" : $"{done * 100 / all.Count} %";

        var used = _local.TotalDownloadedBytes();
        StorageText = $"{Tx.Size(used)} / 1 {Tx.T("Go", "GB")}";
        StorageFraction = Math.Min(1, used / 1073741824.0);

        if (_catalog.FindLecon(_local.LastLessonId) is { } last)
        {
            ContinueTitle = last.Titre;
            ContinueContext = $"{last.MatiereNom} · {last.ChapitreTitre}";
            ContinueProgress = _local.Get(last.Id)?.Avancement ?? 0;
        }
        else
        {
            ContinueTitle = Tx.T("Choisissez une leçon pour commencer", "Pick a lesson to get started");
            ContinueContext = string.Empty;
            ContinueProgress = 0;
        }

        State = Matieres.Count == 0 ? PageState.Empty : PageState.Content;
        _sync.Trigger();
    }

    partial void OnSelectedMatiereChanged(Matiere? value)
    {
        if (value is null) return;
        var id = value.Id;
        SelectedMatiere = null;
        _ = _nav.GoToAsync($"subject?id={id}");
    }

    [RelayCommand]
    private Task OpenReaderAsync()
    {
        var last = _local.LastLessonId;
        if (last is not null && _local.Get(last) is { Downloaded: true }) return _nav.GoToAsync($"reader?id={last}");
        return Matieres.FirstOrDefault() is { } m ? _nav.GoToAsync($"subject?id={m.Id}") : Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenSubjectAsync(string? id)
    {
        id ??= Matieres.FirstOrDefault()?.Id;
        return id is null ? Task.CompletedTask : _nav.GoToAsync($"subject?id={id}");
    }

    [RelayCommand] private Task OpenGuideAsync() => _nav.GoToAsync("help");
    [RelayCommand] private Task OpenExercisesAsync() => _nav.GoToAsync("exercises");
}

[QueryProperty(nameof(SubjectId), "id")]
public partial class SubjectViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly CatalogService _catalog;
    private readonly IDownloadService _downloads;
    private readonly LocalStateService _local;
    private readonly SessionService _session;

    public SubjectViewModel(INavigationService nav, IDialogService dialogs, CatalogService catalog, IDownloadService downloads,
                            LocalStateService local, SessionService session)
    {
        _nav = nav; _dialogs = dialogs; _catalog = catalog; _downloads = downloads; _local = local; _session = session;
        var loc = Loc.Instance;
        FilterOptions = new List<string> { loc["filter_all"], loc["filter_downloaded"], loc["filter_unread"], loc["filter_done"] };
        _matiere = new Matiere { Nom = string.Empty };
    }

    public IReadOnlyList<string> FilterOptions { get; }
    public ObservableCollection<LeconGroup> Groups { get; } = new();

    [ObservableProperty] private string _subtitle = string.Empty;
    [ObservableProperty] private string _downloadSize = "—";
    [ObservableProperty] private string _freeSpace = "—";
    [ObservableProperty] private string? _subjectId;
    [ObservableProperty] private Matiere _matiere;
    [ObservableProperty] private int _filterIndex;
    [ObservableProperty] private Lecon? _selectedLecon;

    partial void OnFilterIndexChanged(int value) => Reload();

    partial void OnSelectedLeconChanged(Lecon? value)
    {
        if (value is null) return;
        var download = value.ShowDownload;
        var id = value.Id;
        SelectedLecon = null;
        _ = _nav.GoToAsync(download ? $"lesson?id={id}&download=1" : $"lesson?id={id}");
    }

    public async Task LoadAsync()
    {
        if (SubjectId is null) return;
        if (Groups.Count == 0) State = PageState.Loading;
        await _catalog.LoadCatalogAsync(_session.User?.ClasseId);
        var tree = _catalog.GetMatiereTree(SubjectId);
        if (tree is null) { State = PageState.Error; return; }
        Matiere = _catalog.ToMatiere(tree);
        Reload();
        State = PageState.Content;
    }

    private void Reload()
    {
        var tree = _catalog.GetMatiereTree(SubjectId);
        if (tree is null) return;

        Func<Lecon, bool> filter = FilterIndex switch
        {
            1 => l => l.Statut is not (LessonStatus.NotDownloaded or LessonStatus.Failed),
            2 => l => l.Statut is LessonStatus.Downloaded or LessonStatus.UpdateAvailable or LessonStatus.InProgress,
            3 => l => l.Statut is LessonStatus.Read or LessonStatus.Completed,
            _ => _ => true
        };
        Groups.Clear();
        foreach (var g in _catalog.GetGroups(SubjectId, filter)) Groups.Add(g);

        var all = tree.Chapitres.SelectMany(c => c.Lecons).ToList();
        Subtitle = Tx.T($"{_catalog.ClasseNom} · {tree.Chapitres.Count} chapitres · {all.Count} leçons",
                        $"{_catalog.ClasseNom} · {tree.Chapitres.Count} chapters · {all.Count} lessons");
        var pending = PendingLessons(tree);
        DownloadSize = Tx.Size(pending.Sum(l => l.TailleOctets));
        FreeSpace = FreeSpaceText();
    }

    private List<ApiLecon> PendingLessons(ApiMatiereTree tree) =>
        tree.Chapitres.SelectMany(c => c.Lecons)
            .Where(l => _local.Get(l.Id) is not { Downloaded: true } loc || loc.Version < l.Version).ToList();

    private static string FreeSpaceText()
    {
        try { return Tx.Size(new DriveInfo(Path.GetPathRoot(FileSystem.AppDataDirectory) ?? "/").AvailableFreeSpace); }
        catch { return "—"; }
    }

    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        var tree = _catalog.GetMatiereTree(SubjectId);
        if (tree is null) return;
        var pending = PendingLessons(tree);
        if (pending.Count == 0)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Toutes les leçons sont déjà téléchargées.", "All lessons are already downloaded."));
            return;
        }

        // EF-16 : taille totale et espace disponible avant tout téléchargement, avec confirmation.
        var size = Tx.Size(pending.Sum(l => l.TailleOctets));
        var ok = await _dialogs.ConfirmAsync(Tx.T("Télécharger la matière", "Download subject"),
            Tx.T($"{pending.Count} leçon(s) · {size}\nEspace libre : {FreeSpaceText()}", $"{pending.Count} lesson(s) · {size}\nFree space: {FreeSpaceText()}"),
            Loc.Instance["download"], Loc.Instance["cancel"]);
        if (!ok) return;

        State = PageState.Loading;
        var r = await _downloads.DownloadAsync(pending.Select(l => l.Id));
        State = PageState.Content;
        Reload();
        Matiere = _catalog.ToMatiere(tree);
        if (!r.Success) await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], r.Message ?? string.Empty);
    }

    [RelayCommand] private Task OpenExercisesAsync() => _nav.GoToAsync("exercises");
    [RelayCommand] private Task SearchAsync() => _nav.GoToAsync("//main/search");
}

[QueryProperty(nameof(Download), "download")]
[QueryProperty(nameof(LessonId), "id")]
public partial class LessonDetailViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly CatalogService _catalog;
    private readonly IDownloadService _downloads;
    private readonly LocalStateService _local;
    private readonly SyncService _sync;
    private readonly SessionService _session;

    public LessonDetailViewModel(INavigationService nav, IDialogService dialogs, CatalogService catalog, IDownloadService downloads,
                                 LocalStateService local, SyncService sync, SessionService session)
    {
        _nav = nav; _dialogs = dialogs; _catalog = catalog; _downloads = downloads; _local = local; _sync = sync; _session = session;
        _lecon = new Lecon();
    }

    [ObservableProperty] private Lecon _lecon;
    [ObservableProperty] private string? _lessonId;
    [ObservableProperty] private string _chapitre = string.Empty;
    [ObservableProperty] private string _dureeLecture = string.Empty;
    [ObservableProperty] private string _taille = string.Empty;
    [ObservableProperty] private string _versionTexte = string.Empty;
    [ObservableProperty] private string _dateMaj = string.Empty;
    [ObservableProperty] private string _storageLine = string.Empty;
    [ObservableProperty] private string? _download;
    [ObservableProperty] private bool _showSheet;
    [ObservableProperty] private bool _wifiOnly = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    private bool _isFavorite;

    public string FavoriteIcon => IsFavorite ? "★" : "☆";

    partial void OnDownloadChanged(string? value) => ShowSheet = value == "1";
    partial void OnLessonIdChanged(string? value) => Load();

    public async Task LoadAsync()
    {
        if (_catalog.FindLecon(LessonId) is null) await _catalog.LoadCatalogAsync(_session.User?.ClasseId);
        Load();
    }

    private void Load()
    {
        if (_catalog.FindLecon(LessonId) is not { } l) return;
        Lecon = _catalog.ToLecon(l);
        Chapitre = l.ChapitreTitre;
        DureeLecture = $"{l.DureeMin} min";
        Taille = Tx.Size(l.TailleOctets);
        VersionTexte = $"v{l.Version}";
        DateMaj = l.DateMiseAJour.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        StorageLine = Tx.Size(_local.TotalDownloadedBytes());
        IsFavorite = Lecon.EstFavori;
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (LessonId is null) return;
        IsFavorite = !IsFavorite;
        var fav = IsFavorite;
        var meta = _catalog.FindLecon(LessonId);
        _local.Update(LessonId, l =>
        {
            l.Favori = fav;
            l.FavoriUpdatedUtc = DateTime.UtcNow;
            if (meta != null) { l.Titre = meta.Titre; l.MatiereNom = meta.MatiereNom; }
        });
        _sync.Trigger();
    }

    [RelayCommand] private void OpenSheet() => ShowSheet = true;
    [RelayCommand] private void CloseSheet() => ShowSheet = false;

    [RelayCommand]
    private async Task ConfirmDownloadAsync()
    {
        ShowSheet = false;
        if (LessonId is null) return;
        State = PageState.Loading;
        var r = await _downloads.DownloadAsync(new[] { LessonId });
        State = PageState.Content;
        Load();
        await _dialogs.ShowMessageAsync(Loc.Instance["app_name"],
            r.Success ? Tx.T("Leçon téléchargée et vérifiée. Elle est disponible hors ligne.", "Lesson downloaded and verified. It is available offline.")
                      : r.Message ?? string.Empty);
    }

    [RelayCommand]
    private async Task ReadAsync()
    {
        if (LessonId is null) return;
        if (_local.Get(LessonId) is not { Downloaded: true })
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Téléchargez d'abord la leçon pour la lire hors ligne.", "Download the lesson first to read it offline."));
            ShowSheet = true;
            return;
        }
        await _nav.GoToAsync($"reader?id={LessonId}");
    }

    [RelayCommand] private Task ExercisesAsync() => _nav.GoToAsync("exercises");
    [RelayCommand] private Task QuizAsync() => _nav.GoToAsync("quiz");
}

[QueryProperty(nameof(LessonId), "id")]
public partial class ReaderViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly LessonStorage _storage;
    private readonly LocalStateService _local;
    private readonly CatalogService _catalog;
    private readonly SyncService _sync;
    private string? _loadedId;
    private double _lastSaved;

    public ReaderViewModel(INavigationService nav, IDialogService dialogs, LessonStorage storage, LocalStateService local,
                           CatalogService catalog, SyncService sync)
    {
        _nav = nav; _dialogs = dialogs; _storage = storage; _local = local; _catalog = catalog; _sync = sync;
    }

    [ObservableProperty] private string? _lessonId;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _chapitreTitre = string.Empty;
    [ObservableProperty] private string _metaText = string.Empty;
    [ObservableProperty] private List<LessonBlock> _blocks = new();
    [ObservableProperty] private bool _hasQuiz;
    /// <summary>Position de lecture à restaurer (0 à 1), EF-28.</summary>
    public double ResumeAt { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private double _sectionProgress;

    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private bool _showSettings;
    [ObservableProperty] private double _readerFontSize = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BookmarkIcon))]
    private bool _isBookmarked;

    public string ProgressText => $"{SectionProgress * 100:0} %";
    public bool IsLight => !IsDark;
    public double TitleSize => ReaderFontSize + 12;
    public string BookmarkIcon => IsBookmarked ? "🔖" : "🏷";

    partial void OnIsDarkChanged(bool value) => OnPropertyChanged(nameof(IsLight));
    partial void OnReaderFontSizeChanged(double value) => OnPropertyChanged(nameof(TitleSize));

    /// <summary>Lit la leçon depuis les fichiers téléchargés : aucune requête réseau (EF-25).</summary>
    public async Task LoadAsync()
    {
        if (LessonId is null || _loadedId == LessonId) return;
        var file = await _storage.ReadLessonAsync(LessonId);
        if (file is null)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Cette leçon n'est pas téléchargée.", "This lesson is not downloaded."));
            await _nav.GoBackAsync();
            return;
        }

        _loadedId = LessonId;
        var meta = _catalog.FindLecon(LessonId);
        var loc = _local.Get(LessonId);
        Title = file.Titre;
        ChapitreTitre = meta?.ChapitreTitre ?? string.Empty;
        MetaText = Tx.T($"{file.DureeMin} min · {file.Sections.Count} sections · version {file.Version}",
                        $"{file.DureeMin} min · {file.Sections.Count} sections · version {file.Version}");
        HasQuiz = File.Exists(Path.Combine(_storage.ContentDir(LessonId), "quiz.json"));
        ResumeAt = loc?.Statut == LessonStatus.Completed ? 0 : loc?.Avancement ?? 0;
        SectionProgress = ResumeAt;
        _lastSaved = ResumeAt;
        Blocks = file.Blocks;

        _local.LastLessonId = LessonId;
        _local.Update(LessonId, l =>
        {
            if (l.Statut is LessonStatus.Downloaded or LessonStatus.NotDownloaded) l.Statut = LessonStatus.InProgress;
            l.UpdatedUtc = DateTime.UtcNow;
        });
    }

    /// <summary>Appelé pendant le défilement ; la leçon passe à « terminée » en atteignant la fin (RG4).</summary>
    public void OnScrolled(double ratio)
    {
        if (LessonId is null) return;
        SectionProgress = ratio;
        var finished = ratio >= 0.95;
        if (!finished && Math.Abs(ratio - _lastSaved) < 0.05) return;
        _lastSaved = ratio;
        Save(ratio, finished);
    }

    private void Save(double ratio, bool completed)
    {
        if (LessonId is null) return;
        _local.Update(LessonId, l =>
        {
            if (completed) { l.Statut = LessonStatus.Completed; l.Avancement = 1; }
            else if (l.Statut != LessonStatus.Completed) { l.Statut = LessonStatus.InProgress; l.Avancement = ratio; }
            l.UpdatedUtc = DateTime.UtcNow;
        });
        if (completed) _sync.Trigger();
    }

    [RelayCommand] private void ToggleSettings() => ShowSettings = !ShowSettings;
    [RelayCommand] private void ToggleBookmark() => IsBookmarked = !IsBookmarked; // TODO BACKEND : IAnnotationService

    [RelayCommand]
    private async Task PreviousAsync()
    {
        if (LessonId is null) return;
        await GoToSiblingAsync(_catalog.GetSiblings(LessonId).Previous, complete: false);
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (LessonId is null) return;
        Save(1, completed: true);
        var next = _catalog.GetSiblings(LessonId).Next;
        if (next is null)
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Bravo : c'est la dernière leçon de cette matière.", "Well done: this is the last lesson of this subject."));
            return;
        }
        await GoToSiblingAsync(next, complete: true);
    }

    private async Task GoToSiblingAsync(string? id, bool complete)
    {
        if (id is null) return;
        if (_local.Get(id) is not { Downloaded: true })
        {
            await _dialogs.ShowMessageAsync(Loc.Instance["app_name"], Tx.T("Cette leçon n'est pas encore téléchargée.", "That lesson is not downloaded yet."));
            return;
        }
        await _nav.GoToAsync($"../reader?id={id}");
    }

    [RelayCommand] private Task QuizAsync() => _nav.GoToAsync("quiz");
    [RelayCommand] private Task ListenAsync() => _dialogs.ShowComingSoonAsync();
}

public partial class SearchViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly MockDataProvider _data;

    public SearchViewModel(INavigationService nav, MockDataProvider data)
    {
        _nav = nav;
        _data = data;
        var loc = Loc.Instance;
        FilterOptions = new List<string> { loc["search_all"], loc["search_lessons"], loc["search_notes"], loc["search_class"] };
        _query = "théorème";
        Apply();
    }

    public IReadOnlyList<string> FilterOptions { get; }
    public ObservableCollection<SearchResult> Results { get; } = new();
    public string CountText => string.Format(Loc.Instance["search_count"], Results.Count);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuery), nameof(NoResults))]
    private string _query;

    [ObservableProperty] private int _filterIndex;
    [ObservableProperty] private SearchResult? _selectedResult;

    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
    public bool NoResults => HasQuery && Results.Count == 0;

    partial void OnQueryChanged(string value)
    {
        Apply();
        OnPropertyChanged(nameof(NoResults));
    }

    partial void OnSelectedResultChanged(SearchResult? value)
    {
        if (value is null) return;
        SelectedResult = null;
        _ = _nav.GoToAsync("reader");
    }

    [RelayCommand] private void Clear() => Query = string.Empty;
    [RelayCommand] private Task OpenAnnalesAsync() => _nav.GoToAsync("exercises");

    // TODO BACKEND : remplacer par IRechercheService (index local plein texte, EF-51/52).
    private void Apply()
    {
        Results.Clear();
        if (!HasQuery) return;
        var q = Normalize(Query);
        foreach (var r in _data.SearchResults.Where(r => Normalize(r.Titre + r.Avant + r.Mot + r.Apres).Contains(q)))
            Results.Add(r);
        OnPropertyChanged(nameof(CountText));
    }

    private static string Normalize(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }
}
