using System.Collections.ObjectModel;
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

public partial class LibraryViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly MockDataProvider _data;

    public LibraryViewModel(INavigationService nav, MockDataProvider data)
    {
        _nav = nav;
        _data = data;
        var loc = Loc.Instance;
        SubjectFilters = new List<string> { loc["lib_all_subjects"], "Mathématiques", "Physique-Chimie", "SVT" };
        Refresh();
    }

    public IReadOnlyList<string> SubjectFilters { get; }
    public ObservableCollection<LibraryItem> Items { get; } = new();

    [ObservableProperty] private bool _isFavoris = true;
    [ObservableProperty] private bool _isNotes;
    [ObservableProperty] private bool _isMarquePages;
    [ObservableProperty] private bool _isARefaire;
    [ObservableProperty] private int _filterIndex;
    [ObservableProperty] private LibraryItem? _selectedItem;

    public string EmptyTitle => Loc.Instance[CurrentTab switch { 0 => "lib_empty_fav", 1 => "lib_empty_notes", 2 => "lib_empty_bm", _ => "lib_empty_redo" }];

    private int CurrentTab => IsFavoris ? 0 : IsNotes ? 1 : IsMarquePages ? 2 : 3;

    partial void OnIsFavorisChanged(bool value) { if (value) Refresh(); }
    partial void OnIsNotesChanged(bool value) { if (value) Refresh(); }
    partial void OnIsMarquePagesChanged(bool value) { if (value) Refresh(); }
    partial void OnIsARefaireChanged(bool value) { if (value) Refresh(); }
    partial void OnFilterIndexChanged(int value) => Refresh();

    partial void OnSelectedItemChanged(LibraryItem? value)
    {
        if (value is null) return;
        SelectedItem = null;
        _ = _nav.GoToAsync("reader");
    }

    private void Refresh()
    {
        Items.Clear();
        var subject = FilterIndex > 0 ? SubjectFilters[FilterIndex] : null;
        foreach (var item in _data.GetLibrary(CurrentTab).Where(i => subject is null || i.Matiere == subject))
            Items.Add(item);
        OnPropertyChanged(nameof(EmptyTitle));
    }
}

public partial class ProgressViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly CatalogService _catalog;
    private readonly SessionService _session;
    private readonly LocalStateService _local;

    public ProgressViewModel(INavigationService nav, CatalogService catalog, SessionService session, LocalStateService local)
    {
        _nav = nav; _catalog = catalog; _session = session; _local = local;
    }

    public ObservableCollection<Matiere> Matieres { get; } = new();

    [ObservableProperty] private double _global;
    public string StudyTime => "—";
    public int Streak => 0;
    public string GoalText => "—";
    public double GoalFraction => 0;
    public string ExercisesDone => "—";
    public string ExerciseAverage => "—";
    public string WeakChapter => "—";

    [ObservableProperty] private Matiere? _selectedMatiere;

    /// <summary>Progression calculée localement (RG5 : leçons terminées / leçons publiées), donc disponible hors ligne.</summary>
    public async Task LoadAsync()
    {
        await _catalog.LoadCatalogAsync(_session.User?.ClasseId);
        Matieres.Clear();
        foreach (var t in _catalog.GetMatiereTrees()) Matieres.Add(_catalog.ToMatiere(t));
        var all = _catalog.AllLecons();
        var done = all.Count(l => _local.Get(l.Id) is { Statut: LessonStatus.Completed });
        Global = all.Count == 0 ? 0 : (double)done / all.Count;
    }

    partial void OnSelectedMatiereChanged(Matiere? value)
    {
        if (value is null) return;
        var id = value.Id;
        SelectedMatiere = null;
        _ = _nav.GoToAsync($"subject?id={id}");
    }
}

public partial class StorageViewModel : BaseViewModel
{
    public StorageViewModel(MockDataProvider data) => Items = data.StorageItems;

    public IReadOnlyList<StorageItem> Items { get; }
    public string UsedText => "166 Mo";
    public string FreeText => "1,8 Go";
    public double UsedFraction => 0.17;
}

public partial class QuizViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly MockDataProvider _data;

    public QuizViewModel(INavigationService nav, MockDataProvider data)
    {
        _nav = nav;
        _data = data;
        Corrections = data.QuizQuestions.Select((q, i) => new ResultItem
        {
            Numero = i + 1,
            Enonce = q.Enonce,
            Reponse = q.Options[i == 1 ? 0 : q.IndexBonneReponse],
            BonneReponse = q.Options[q.IndexBonneReponse],
            Explication = q.Explication,
            Verdict = i == 1 ? Verdict.Incorrect : Verdict.Correct
        }).ToList();
    }

    public IReadOnlyList<ResultItem> Corrections { get; }
    public int Total => _data.QuizQuestions.Count;
    public string ScoreText => $"2 / {Total}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQuestion), nameof(IsResult), nameof(Current), nameof(ProgressText), nameof(ProgressValue), nameof(CanGoBack))]
    private int _index;

    public bool IsQuestion => Index < Total;
    public bool IsResult => !IsQuestion;
    public bool CanGoBack => Index > 0;
    public Question Current => _data.QuizQuestions[Math.Min(Index, Total - 1)];
    public string ProgressText => $"{Math.Min(Index + 1, Total)}/{Total}";
    public double ProgressValue => (double)Math.Min(Index + 1, Total) / Total;

    [RelayCommand] private void Next() { if (Index < Total) Index++; }  // TODO BACKEND : IQuizService
    [RelayCommand] private void Previous() { if (Index > 0) Index--; }
    [RelayCommand] private void Restart() => Index = 0;
    [RelayCommand] private Task CloseAsync() => _nav.GoBackAsync();
}
