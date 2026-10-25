using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Interfaces;
using EduHorsLigne.Services.Mock;

namespace EduHorsLigne.ViewModels;

/// <summary>Ligne d'exercice dans la liste d'un chapitre (porte sa propre commande d'ouverture).</summary>
public partial class ExerciseRow : ObservableObject
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;

    public ExerciseRow(Exercice item, INavigationService nav, IDialogService dialogs)
    {
        Item = item;
        _nav = nav;
        _dialogs = dialogs;
    }

    public Exercice Item { get; }

    [RelayCommand]
    private Task OpenAsync()
    {
        if (!Item.Telecharge) return _dialogs.ShowComingSoonAsync(); // TODO BACKEND : IDownloadService
        return _nav.GoToAsync(Item.Corrige ? "correction" : "session");
    }
}

/// <summary>Chapitre repliable de la liste « Exercices par chapitre ».</summary>
public partial class ExerciseChapter : ObservableObject
{
    public ExerciseChapter(int numero, string titre, string resume, IEnumerable<ExerciseRow> rows)
    {
        Numero = numero;
        Titre = titre;
        Resume = resume;
        Rows = rows.ToList();
    }

    public int Numero { get; }
    public string Titre { get; }
    public string Resume { get; }
    public IReadOnlyList<ExerciseRow> Rows { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Chevron))]
    private bool _isExpanded = true;

    public string Chevron => IsExpanded ? "⌃" : "⌄";

    [RelayCommand] private void Toggle() => IsExpanded = !IsExpanded;
}

public partial class ExercisesHomeViewModel : BaseViewModel
{
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;

    public ExercisesHomeViewModel(INavigationService nav, IDialogService dialogs, MockDataProvider data)
    {
        _nav = nav;
        _dialogs = dialogs;
        var loc = Loc.Instance;
        FilterItems = new List<string> { loc["ex_filter_all"], "Baccalauréat", "Probatoire", "BEPC" };
        var ch1 = data.Exercices.Where(e => e.ChapitreTitre.StartsWith("Limites"));
        var ch2 = data.Exercices.Where(e => e.ChapitreTitre.StartsWith("Dérivation"));
        Chapters = new List<ExerciseChapter>
        {
            new(1, "Limites et continuité", "4 exercices disponibles", ch1.Select(e => new ExerciseRow(e, nav, dialogs))),
            new(2, "Dérivation et primitives", "5 exercices disponibles", ch2.Select(e => new ExerciseRow(e, nav, dialogs))),
        };
    }

    public IReadOnlyList<string> FilterItems { get; }
    public IReadOnlyList<ExerciseChapter> Chapters { get; }
    public string StorageText => "142 Mo / 500 Mo";

    [ObservableProperty] private int _filterIndex;

    [RelayCommand] private Task DownloadPackAsync() => _dialogs.ShowComingSoonAsync(); // TODO BACKEND : IDownloadService
    [RelayCommand] private Task ResumeSessionAsync() => _nav.GoToAsync("session");
    [RelayCommand] private Task SearchAsync() => _nav.GoToAsync("//main/search");
    [RelayCommand] private Task SeeAllAsync() => _dialogs.ShowComingSoonAsync();
}

public partial class ExerciseCorrectionViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ExerciseCorrectionViewModel(INavigationService nav)
    {
        _nav = nav;
        var loc = Loc.Instance;
        TabItems = new List<string> { loc["tab_statement"], loc["tab_solution"], loc["tab_method"] };
        Steps = new List<CorrectionStep>
        {
            new() { Titre = "Étape 1 · Continuité sur [1 ; 2]", Points = "+0,75 pt", Formule = "f(x) = x³ + 2x − 5",
                    Texte = "f est une fonction polynôme, donc elle est continue et dérivable sur ℝ, en particulier sur l'intervalle [1 ; 2]." },
            new() { Titre = "Étape 2 · Dérivabilité & stricte monotonie", Points = "+1,25 pt", Formule = "f'(x) = 3x² + 2 > 0,   ∀ x ∈ [1 ; 2]",
                    Texte = "Pour tout x ∈ [1 ; 2], 3x² ≥ 0 donc 3x² + 2 ≥ 2 > 0. La dérivée f'(x) est strictement positive, donc f est strictement croissante sur [1 ; 2]." },
            new() { Titre = "Étape 3 · Calcul des images aux bornes", Points = "+1,00 pt", Formule = "f(1) = −2 < 0     f(2) = 7 > 0",
                    Texte = "On a f(1) · f(2) < 0. Par conséquent, 0 est strictement compris entre f(1) et f(2)." },
            new() { Titre = "Étape 4 · Conclusion par le Corollaire du TVI", Points = "+1,00 pt", Conclusion = true,
                    Texte = "« D'après le corollaire du théorème des valeurs intermédiaires (théorème de la bijection), la fonction f étant continue et strictement croissante sur [1 ; 2], l'équation f(x) = 0 admet une unique solution α ∈ [1 ; 2]. »" },
        };
    }

    public IReadOnlyList<string> TabItems { get; }
    public IReadOnlyList<CorrectionStep> Steps { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatement), nameof(IsSolution), nameof(IsMethod), nameof(ShowSteps))]
    private int _tabIndex = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSteps), nameof(MaskLabel))]
    private bool _isMasked;

    public bool IsStatement => TabIndex == 0;
    public bool IsSolution => TabIndex == 1;
    public bool IsMethod => TabIndex == 2;
    public bool ShowSteps => IsSolution && !IsMasked;
    public string MaskLabel => IsMasked ? Loc.Instance["show"] : Loc.Instance["mask"];

    [RelayCommand] private void ToggleMask() => IsMasked = !IsMasked;
    [RelayCommand] private Task NextAsync() => _nav.GoToAsync("session");
}

public partial class ExerciseSessionViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ExerciseSessionViewModel(INavigationService nav)
    {
        _nav = nav;
        var loc = Loc.Instance;
        TabItems = new List<string> { loc["tab_statement_q"], loc["tab_method"], loc["tab_formulas"] };
    }

    public IReadOnlyList<string> TabItems { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStatement), nameof(IsMethod), nameof(IsFormulas))]
    private int _tabIndex;

    [ObservableProperty] private string _draft = string.Empty;
    [ObservableProperty] private bool _hintShown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PauseIcon))]
    private bool _isPaused;

    public bool IsStatement => TabIndex == 0;
    public bool IsMethod => TabIndex == 1;
    public bool IsFormulas => TabIndex == 2;
    public string PauseIcon => IsPaused ? "▶" : "⏸";

    [RelayCommand] private void TogglePause() => IsPaused = !IsPaused;      // TODO BACKEND : chronomètre réel (EF-99)
    [RelayCommand] private void ToggleHint() => HintShown = !HintShown;     // TODO BACKEND : pénalité de barème (EF-95)
    [RelayCommand] private void Clear() => Draft = string.Empty;
    [RelayCommand] private void InsertAlpha() => Draft += "α";
    [RelayCommand] private void InsertInterval() => Draft += "[a ; b]";
    [RelayCommand] private Task OfficialAsync() => _nav.GoToAsync("correction");
    [RelayCommand] private Task ValidateAsync() => _nav.GoToAsync("result");
}

public partial class ExerciseResultViewModel : BaseViewModel
{
    private readonly INavigationService _nav;

    public ExerciseResultViewModel(INavigationService nav)
    {
        _nav = nav;
        Ring = new RingDrawable(3.75 / 4.5);
        Rows = new List<ResultRow>
        {
            new() { Titre = "Question 1", Detail = "Définition & vérification de la continuité", Points = "1,0 / 1,0 pt" },
            new() { Titre = "Question 2.a", Detail = "Dérivée et sens de variation sur l'intervalle", Points = "1,25 / 1,25 pt" },
            new() { Titre = "Question 2.b", Detail = "Application du TVI et encadrement", Points = "1,0 / 1,75 pt", Reussi = false,
                    Remarque = "Oubli de la mention explicite de la stricte monotonie pour l'unicité de la solution." },
            new() { Titre = "Question 3", Detail = "Interprétation graphique de l'asymptote oblique", Points = "0,5 / 0,5 pt" },
        };
    }

    public IDrawable Ring { get; }
    public IReadOnlyList<ResultRow> Rows { get; }
    public string ScoreText => "3,75";

    [RelayCommand] private Task NextExerciseAsync() => _nav.GoToAsync("exercises");
    [RelayCommand] private Task SolutionAsync() => _nav.GoToAsync("correction");
    [RelayCommand] private Task RestartAsync() => _nav.GoToAsync("session");
    [RelayCommand] private Task OpenMethodAsync() => _nav.GoToAsync("correction");
}
