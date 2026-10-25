namespace EduHorsLigne.Models;

// Modèles neutres (POCO) : aucune dépendance à l'interface, réutilisables côté API et SQLite.

/// <summary>Base commune : identifiant, numéro de version et dates UTC.</summary>
public abstract class EntityBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Version { get; set; } = 1;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public enum SousSysteme { Francophone, Anglophone }
public enum LessonStatus { NotDownloaded, Downloaded, Read, InProgress, Completed, UpdateAvailable, Failed }
public enum Difficulty { Easy, Medium, Hard }
public enum ExerciseStatus { NotStarted, InProgress, Done }
public enum QuestionType { SingleChoice, MultipleChoice, TrueFalse, FillBlank, ShortAnswer, Essay }
public enum Verdict { Correct, Partial, Incorrect }
public enum BlockType { Heading, Paragraph, Definition, Example, BulletList, Table, Formula }

public class Eleve : EntityBase
{
    public string Prenom { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public SousSysteme SousSysteme { get; set; }
    public string Classe { get; set; } = string.Empty;
    public string Langue { get; set; } = "fr";
    public string Initiales => ((Prenom.Length > 0 ? Prenom[..1] : "") + (Nom.Length > 0 ? Nom[..1] : "")).ToUpperInvariant();
}

public class Classe : EntityBase
{
    public string Nom { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public SousSysteme SousSysteme { get; set; }
}

public class Matiere : EntityBase
{
    public string Nom { get; set; } = string.Empty;
    public string Initiale { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    /// <summary>Ton de la pastille d'icône : Ok, Warn, Info, Neutral.</summary>
    public string Ton { get; set; } = "Neutral";
    public string Meta { get; set; } = string.Empty;
    public int NbLecons { get; set; }
    public int NbTerminees { get; set; }
    public double TailleMo { get; set; }
    /// <summary>Avancement de 0 à 1 (RG5 : leçons terminées / leçons publiées).</summary>
    public double Progression { get; set; }
    /// <summary>Vrai si aucune leçon de la matière n'est téléchargée.</summary>
    public bool HorsLigneVide { get; set; }
    public bool ADesLecons => !HorsLigneVide;
    public string Resume => $"{NbTerminees}/{NbLecons}";
}

public class Chapitre : EntityBase
{
    public string MatiereId { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public int Ordre { get; set; }
}

public class Lecon : EntityBase
{
    public string ChapitreId { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string ChapitreTitre { get; set; } = string.Empty;
    public string MatiereNom { get; set; } = string.Empty;
    public int DureeMin { get; set; }
    public double TailleMo { get; set; }
    /// <summary>État local de la leçon (sera porté par Telechargement/Progression côté backend).</summary>
    public LessonStatus Statut { get; set; }
    public bool EstFavori { get; set; }
    /// <summary>Avancement de lecture de 0 à 1.</summary>
    public double Avancement { get; set; }

    public string EtatTexte => Statut switch
    {
        LessonStatus.InProgress => $"En cours · {Avancement * 100:0} %",
        LessonStatus.UpdateAvailable => $"Version {Version} disponible",
        LessonStatus.Failed => "Échec du téléchargement",
        LessonStatus.NotDownloaded => "Non téléchargé",
        _ => "Téléchargé"
    };
    public string TailleTexte => TailleMo < 0.1 ? $"{TailleMo * 1024:0} Ko" : $"{TailleMo:0.0} Mo";
    public string Meta => $"{DureeMin} min · {TailleTexte} · {EtatTexte}";
    public string IconeGlyph => Statut switch
    {
        LessonStatus.Read or LessonStatus.Completed or LessonStatus.Downloaded => "✓",
        LessonStatus.InProgress => "◷",
        LessonStatus.UpdateAvailable => "↻",
        _ => "☁"
    };
    public string IconeTon => Statut switch
    {
        LessonStatus.Read or LessonStatus.Completed or LessonStatus.Downloaded => "Ok",
        LessonStatus.InProgress => "Info",
        LessonStatus.UpdateAvailable => "Warn",
        _ => "Neutral"
    };
    public bool ShowDownload => Statut is LessonStatus.NotDownloaded or LessonStatus.Failed;
    public bool ShowPlay => Statut == LessonStatus.InProgress;
    public bool ShowBadge => Statut is LessonStatus.Read or LessonStatus.UpdateAvailable or LessonStatus.Completed;
    public bool ShowProgress => Statut == LessonStatus.InProgress;
}

/// <summary>Groupe de leçons d'un chapitre (pour les listes groupées).</summary>
public class LeconGroup : List<Lecon>
{
    public LeconGroup(string titre, IEnumerable<Lecon> lecons, int total, bool actif) : base(lecons)
    {
        Titre = titre;
        Total = total;
        Actif = actif;
    }
    public string Titre { get; }
    public int Total { get; }
    /// <summary>Premier chapitre (pastille verte) ou suivants (pastille grise).</summary>
    public bool Actif { get; }
    public bool Inactif => !Actif;
    public string Resume => $"{Total} leçons";
}

public class TableRow
{
    public string C1 { get; set; } = string.Empty;
    public string C2 { get; set; } = string.Empty;
    public string C3 { get; set; } = string.Empty;
}

/// <summary>Bloc de contenu structuré d'une leçon (titre, définition, formule, tableau...).</summary>
public class LessonBlock
{
    public BlockType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public List<TableRow> Rows { get; set; } = new();
}

public class MarquePage : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string LeconId { get; set; } = string.Empty;
    public double Position { get; set; }
    public string Libelle { get; set; } = string.Empty;
}

public class Note : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string LeconId { get; set; } = string.Empty;
    public double Position { get; set; }
    public string Texte { get; set; } = string.Empty;
}

public class Favori : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string LeconId { get; set; } = string.Empty;
    public string? Collection { get; set; }
}

public class Progression : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string LeconId { get; set; } = string.Empty;
    public LessonStatus Statut { get; set; }
    public double PourcentageLecture { get; set; }
    public int TempsPasseSec { get; set; }
    public double DernierePosition { get; set; }
}

public class Question : EntityBase
{
    public string Enonce { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int IndexBonneReponse { get; set; }
    public string Explication { get; set; } = string.Empty;
}

public class Quiz : EntityBase
{
    public string LeconId { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public List<Question> Questions { get; set; } = new();
}

public class ResultatQuiz : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string QuizId { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class Exercice : EntityBase
{
    public string LeconId { get; set; } = string.Empty;
    public string ChapitreId { get; set; } = string.Empty;
    public int Numero { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string ChapitreTitre { get; set; } = string.Empty;
    public string MatiereNom { get; set; } = string.Empty;
    public Difficulty Difficulte { get; set; }
    public int DureeMin { get; set; }
    public double Points { get; set; }
    public ExerciseStatus Statut { get; set; }
    public bool Telecharge { get; set; } = true;
    public double TailleMo { get; set; }
    public bool Corrige { get; set; }
    public double? PointsObtenus { get; set; }

    public string Etiquette => $"EXERCICE {Numero}";
    public string DureeTexte => $"{DureeMin} min";
    public string PointsTexte => $"{Points:0.##} pts";
    public string Resultat => PointsObtenus is { } p ? $"Traité {p:0.##} / {Points:0.##} pts" : string.Empty;
    public bool ShowResultat => PointsObtenus is not null;
    public bool ShowEtatTexte => PointsObtenus is null;
    public string EtatTexte => !Telecharge ? "Non téléchargé" : "Non commencé";
    public bool ShowArrow => Telecharge && PointsObtenus is null;
    public bool ShowDone => PointsObtenus is not null;
    public bool ShowDownload => !Telecharge;
    public string TailleTexte => $"{TailleMo:0.0} Mo";
}

public class QuestionExercice : EntityBase
{
    public string ExerciceId { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Enonce { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public List<string> BonnesReponses { get; set; } = new();
    public List<string> Indices { get; set; } = new();
    public string Explication { get; set; } = string.Empty;
    public double Bareme { get; set; } = 1;
}

public class TentativeExercice : EntityBase
{
    public string EleveId { get; set; } = string.Empty;
    public string ExerciceId { get; set; } = string.Empty;
    public DateTime DebutUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public double Score { get; set; }
    public int TempsPasseSec { get; set; }
}

public class ReponseExercice : EntityBase
{
    public string TentativeId { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public string Reponse { get; set; } = string.Empty;
    public bool Correcte { get; set; }
    public int IndicesUtilises { get; set; }
}

public class Signalement : EntityBase
{
    public string LeconId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Envoye { get; set; }
}

// ----- Objets d'affichage (DTO) utilisés par les écrans -----

public class LibraryItem
{
    public string Icone { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string SousTitre { get; set; } = string.Empty;
    public string Matiere { get; set; } = string.Empty;
}

public class SearchResult
{
    public string Titre { get; set; } = string.Empty;
    public string Avant { get; set; } = string.Empty;
    public string Mot { get; set; } = string.Empty;
    public string Apres { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceIcone { get; set; } = string.Empty;
    public string Etiquette { get; set; } = string.Empty;
    public string EtiquetteIcone { get; set; } = string.Empty;
    public string EtiquetteTon { get; set; } = "Ok";
}

public class StorageItem
{
    public string Nom { get; set; } = string.Empty;
    public double TailleMo { get; set; }
    public double Fraction { get; set; }
    public string Taille => $"{TailleMo:0} Mo";
}

public class FaqItem
{
    public string Question { get; set; } = string.Empty;
    public string Reponse { get; set; } = string.Empty;
}

public class AttemptItem
{
    public string Date { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Duree { get; set; } = string.Empty;
}

public class ResultItem
{
    public int Numero { get; set; }
    public string Enonce { get; set; } = string.Empty;
    public string Reponse { get; set; } = string.Empty;
    public string BonneReponse { get; set; } = string.Empty;
    public string Explication { get; set; } = string.Empty;
    public Verdict Verdict { get; set; }
}

/// <summary>Étape du corrigé pas à pas d'un exercice.</summary>
public class CorrectionStep
{
    public string Titre { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string Formule { get; set; } = string.Empty;
    public string Texte { get; set; } = string.Empty;
    public bool Conclusion { get; set; }
    public bool Normale => !Conclusion;
    public bool ADesFormule => !string.IsNullOrEmpty(Formule);
}

/// <summary>Ligne du détail par question d'une correction.</summary>
public class ResultRow
{
    public string Titre { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public bool Reussi { get; set; } = true;
    public bool Partiel => !Reussi;
    public string Remarque { get; set; } = string.Empty;
}
