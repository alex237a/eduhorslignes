using EduHorsLigne.Api.Domain;

namespace EduHorsLigne.Api.Common;

/// <summary>Format d'erreur unique de l'API (le front l'affiche tel quel, champ par champ si Fields est renseigné).</summary>
public record ApiError(string Code, string Message, Dictionary<string, string>? Fields = null,
                       int? TentativesRestantes = null, int? RetryAfterSeconds = null);

// ----- Contenu d'une leçon (même structure que LessonBlock / TableRow / Question du front) -----
public record TableRowDto(string C1, string C2, string C3);
public record LessonBlockDto(BlockType Type, string? Text = null, List<string>? Items = null, List<TableRowDto>? Rows = null);
public record QuestionDto(string Enonce, List<string> Options, int IndexBonneReponse, string? Explication = null);
public record QuizDto(string? Titre, List<QuestionDto> Questions);

// ----- Auth -----
public record RegisterRequest(string? Nom, string? Prenom, string? Identifiant, string? MotDePasse,
                              string? ConfirmationMotDePasse, string? ClasseId, string? Langue, bool AccepteConditions);
public record LoginRequest(string? Identifiant, string? MotDePasse);
public record ForgotPasswordRequest(string? Identifiant);
public record VerifyCodeRequest(string? Identifiant, string? Code);
public record ResetPasswordRequest(string? Identifiant, string? Code, string? NouveauMotDePasse);
public record UserDto(string Id, string Prenom, string Nom, string Identifiant, SousSysteme? SousSysteme,
                      string? ClasseId, string? ClasseNom, string Langue, string Role);
public record AuthResponse(string Token, DateTime ExpiresUtc, UserDto Utilisateur);
public record UpdateProfileRequest(string? Prenom, string? Nom, string? ClasseId, string? Langue);

// ----- Catalogue -----
public record ClasseDto(string Id, string Nom, string Cycle, SousSysteme SousSysteme, int NbMatieres);
public record MatiereDto(string Id, string ClasseId, string Nom, string Initiale, string Icone, string Ton,
                         int NbChapitres, int NbLecons, long TailleOctets)
{
    public double TailleMo => Math.Round(TailleOctets / 1048576.0, 2);
}
public record ChapitreDto(string Id, string MatiereId, string Titre, int Ordre, int NbLecons);
public record LeconDto(string Id, string ChapitreId, string ChapitreTitre, string MatiereId, string MatiereNom,
                       string Titre, int Ordre, int DureeMin, long TailleOctets, int Version, string? Checksum,
                       DateTime DateMiseAJour, bool AQuiz)
{
    public double TailleMo => Math.Round(TailleOctets / 1048576.0, 2);
}
public record ChapitreTree(ChapitreDto Chapitre, List<LeconDto> Lecons);
public record MatiereTree(MatiereDto Matiere, List<ChapitreTree> Chapitres);
public record ClasseTree(ClasseDto Classe, List<MatiereTree> Matieres);
public record CatalogDto(DateTime GenereUtc, List<ClasseTree> Classes);

// ----- Téléchargement / mises à jour -----
public record ChecksumDto(string LeconId, int Version, string? Checksum, string Algorithme, long TailleOctets);
public record BatchRequest(List<string>? LeconIds);
public record ClientLessonVersion(string Id, int Version);
public record UpdateInfo(string Id, string Etat, int VersionServeur, string? Checksum, long TailleOctets);

// ----- Retours -----
public record ReportRequest(string? Id, string? Type, string? Message);
public record RatingRequest(bool Utile);

// ----- Administration -----
public record ClasseUpsert(string? Nom, string? Cycle, SousSysteme? SousSysteme, int? Ordre);
public record MatiereUpsert(string? ClasseId, string? Nom, string? Initiale, string? Icone, string? Ton);
public record ChapitreUpsert(string? MatiereId, string? Titre, int? Ordre);
public record LeconUpsert(string? ChapitreId, string? Titre, int? Ordre, int? DureeMin, List<LessonBlockDto>? Blocs);
public record PublishRequest(string? NoteVersion);
public record AdminMatiereDto(string Id, string ClasseId, string ClasseNom, string Nom, string Initiale, string Icone, string Ton);
public record AdminChapitreDto(string Id, string MatiereId, string MatiereNom, string ClasseNom, string Titre, int Ordre);
public record AdminLeconDto(string Id, string ChapitreId, string ChapitreTitre, string MatiereNom, string ClasseNom,
                            string Titre, int Ordre, int DureeMin, PublicationStatus Statut, int Version, int VersionSuivante,
                            string? Checksum, long TailleOctets, int Telechargements, DateTime? PublieeUtc, DateTime UpdatedUtc,
                            bool AQuiz, List<LessonBlockDto>? Blocs = null, QuizDto? Quiz = null);
public record TopLecon(string Id, string Titre, int Telechargements);
public record StatsDto(int Eleves, int LeconsPubliees, int TotalTelechargements, double TauxCompletion,
                       int SignalementsNouveaux, int SignalementsTotal, List<TopLecon> PlusTelechargees);
public record SignalementDto(string Id, string LeconId, string? LeconTitre, string Type, string Message, ReportState Etat, DateTime CreatedUtc);

// ----- Synchronisation (EF-63) : dernière modification gagnante -----
public record SyncMarquePage(string Id, string LeconId, double Position, string? Libelle, DateTime UpdatedUtc, bool IsDeleted);
public record SyncNote(string Id, string LeconId, double Position, string? Texte, DateTime UpdatedUtc, bool IsDeleted);
public record SyncFavori(string LeconId, string? Collection, DateTime UpdatedUtc, bool IsDeleted);
public record SyncProgression(string LeconId, ProgressStatus Statut, double PourcentageLecture, int TempsPasseSec,
                              double DernierePosition, DateTime UpdatedUtc);
public record SyncRequest(DateTime? SinceUtc, List<SyncMarquePage>? MarquePages, List<SyncNote>? Notes,
                          List<SyncFavori>? Favoris, List<SyncProgression>? Progressions);
public record SyncResponse(DateTime ServerTimeUtc, int Appliques, int Ignores, List<SyncMarquePage> MarquePages,
                           List<SyncNote> Notes, List<SyncFavori> Favoris, List<SyncProgression> Progressions);

public static class Mapper
{
    public static LeconDto ToDto(Lecon l, string chapitreTitre, string matiereId, string matiereNom) =>
        new(l.Id, l.ChapitreId, chapitreTitre, matiereId, matiereNom, l.Titre, l.Ordre, l.DureeMin,
            l.TailleOctets, l.Version, l.Checksum, l.PublishedUtc ?? l.UpdatedUtc, l.HasQuiz);
}
