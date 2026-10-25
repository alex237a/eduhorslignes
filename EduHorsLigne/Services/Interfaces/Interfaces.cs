using EduHorsLigne.Models;

namespace EduHorsLigne.Services.Interfaces;

// Contrats que le backend C# devra implémenter. Les ViewModels y feront appel à la place du MockDataProvider.
// Chaque méthode est asynchrone et annulable. Pour l'instant seuls INavigationService et IDialogService sont implémentés.

/// <summary>Résultat explicite d'un appel de service.</summary>
public record ServiceResult<T>(bool Success, T? Data = default, string? Message = null, int? AttemptsLeft = null, bool Offline = false);

/// <summary>Navigation Shell (routes relatives ou absolues).</summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoBackAsync();
}

/// <summary>Dialogues d'information et de confirmation.</summary>
public interface IDialogService
{
    Task ShowComingSoonAsync();
    Task ShowMessageAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public interface IAuthService
{
    Task<ServiceResult<Eleve>> LoginAsync(string identifiant, string motDePasse, CancellationToken ct = default);
    Task<ServiceResult<Eleve>> RegisterAsync(Eleve profil, string classeId, string identifiant, string motDePasse, CancellationToken ct = default);
    Task<ServiceResult<bool>> SendResetCodeAsync(string identifiant, CancellationToken ct = default);
    Task<ServiceResult<bool>> VerifyCodeAsync(string identifiant, string code, CancellationToken ct = default);
    Task<ServiceResult<bool>> SetNewPasswordAsync(string identifiant, string code, string nouveauMotDePasse, CancellationToken ct = default);
    Task LogoutAsync(bool effacerDonneesLocales, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAccountAsync(CancellationToken ct = default);
}

public interface ICatalogueService
{
    Task<IReadOnlyList<Matiere>> GetMatieresAsync(string classeId, CancellationToken ct = default);
    Task<IReadOnlyList<Chapitre>> GetChapitresAsync(string matiereId, CancellationToken ct = default);
    Task<IReadOnlyList<Lecon>> GetLeconsAsync(string chapitreId, CancellationToken ct = default);
}

public interface IDownloadService
{
    Task<ServiceResult<bool>> DownloadAsync(IEnumerable<string> leconIds, IProgress<double>? progress = null, CancellationToken ct = default);
    Task PauseAsync(string leconId, CancellationToken ct = default);
    Task ResumeAsync(string leconId, CancellationToken ct = default);
}

public interface ILectureService
{
    Task<IReadOnlyList<LessonBlock>> GetContenuAsync(string leconId, CancellationToken ct = default);
    Task SaveLastPositionAsync(string leconId, double position, CancellationToken ct = default);
}

public interface IAnnotationService
{
    Task<IReadOnlyList<Note>> GetNotesAsync(string? leconId = null, CancellationToken ct = default);
    Task<IReadOnlyList<MarquePage>> GetMarquePagesAsync(string? leconId = null, CancellationToken ct = default);
    Task SaveNoteAsync(Note note, CancellationToken ct = default);
    Task SaveMarquePageAsync(MarquePage marquePage, CancellationToken ct = default);
}

public interface IFavorisService
{
    Task<IReadOnlyList<Lecon>> GetFavorisAsync(CancellationToken ct = default);
    Task SetFavoriAsync(string leconId, bool favori, CancellationToken ct = default);
}

public interface IProgressionService
{
    Task<double> GetProgressionGlobaleAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Progression>> GetProgressionsAsync(CancellationToken ct = default);
    Task ResetMatiereAsync(string matiereId, CancellationToken ct = default);
}

public interface IRechercheService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string terme, string? filtre = null, CancellationToken ct = default);
}

public interface IQuizService
{
    Task<Quiz?> GetQuizAsync(string leconId, CancellationToken ct = default);
    Task SaveResultAsync(ResultatQuiz resultat, CancellationToken ct = default);
}

public interface IExerciceService
{
    Task<IReadOnlyList<Exercice>> GetExercicesAsync(string? matiereId = null, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionExercice>> GetQuestionsAsync(string exerciceId, CancellationToken ct = default);
    Task<TentativeExercice> StartAttemptAsync(string exerciceId, CancellationToken ct = default);
    Task SaveReponseAsync(ReponseExercice reponse, CancellationToken ct = default);
    Task<IReadOnlyList<TentativeExercice>> GetHistoryAsync(string exerciceId, CancellationToken ct = default);
}

public interface IStorageService
{
    Task<IReadOnlyList<StorageItem>> GetUsageAsync(CancellationToken ct = default);
    /// <summary>Supprime les fichiers d'une matière ; notes, marque-pages, favoris et progression sont conservés (EF-20).</summary>
    Task DeleteMatiereFilesAsync(string matiereId, CancellationToken ct = default);
}

public interface ISyncService
{
    Task<ServiceResult<bool>> SyncAsync(CancellationToken ct = default);
}

public interface IParametresService
{
    string Langue { get; set; }
    string Theme { get; set; }
    double TailleTexte { get; set; }
    bool WifiUniquement { get; set; }
    bool NotificationsActives { get; set; }
    bool PinActif { get; set; }
}
