namespace EduHorsLigne.Api.Domain;

public enum UserRole { Eleve, Admin }
public enum SousSysteme { Francophone, Anglophone }
/// <summary>Cycle de vie d'une leçon (EF-75).</summary>
public enum PublicationStatus { Brouillon, EnRelecture, Publiee, Archivee }
public enum ProgressStatus { NonCommencee, EnCours, Terminee }
public enum BlockType { Heading, Paragraph, Definition, Example, BulletList, Table, Formula }
public enum ReportState { Nouveau, Traite }

public abstract class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public class User : Entity
{
    public string Prenom { get; set; } = "";
    public string Nom { get; set; } = "";
    /// <summary>E-mail (minuscules) ou numéro normalisé +237XXXXXXXXX. Unique.</summary>
    public string Identifiant { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Eleve;
    public SousSysteme? SousSysteme { get; set; }
    public string? ClasseId { get; set; }
    public string Langue { get; set; } = "fr";
    public DateTime? ConsentementUtc { get; set; }

    // Anti-force brute (EF-86)
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    // Récupération du mot de passe (EF-82) : code à usage unique, stocké haché
    public string? ResetCodeHash { get; set; }
    public DateTime? ResetCodeExpiresUtc { get; set; }
    public int ResetCodeAttempts { get; set; }
}

public class Classe : Entity
{
    public string Nom { get; set; } = "";
    public string Cycle { get; set; } = "";
    public SousSysteme SousSysteme { get; set; }
    public int Ordre { get; set; }
    public List<Matiere> Matieres { get; set; } = new();
}

public class Matiere : Entity
{
    public string ClasseId { get; set; } = "";
    public Classe Classe { get; set; } = null!;
    public string Nom { get; set; } = "";
    public string Initiale { get; set; } = "";
    public string Icone { get; set; } = "";
    /// <summary>Ok, Warn, Info, Neutral (même vocabulaire que le front).</summary>
    public string Ton { get; set; } = "Neutral";
    public List<Chapitre> Chapitres { get; set; } = new();
}

public class Chapitre : Entity
{
    public string MatiereId { get; set; } = "";
    public Matiere Matiere { get; set; } = null!;
    public string Titre { get; set; } = "";
    public int Ordre { get; set; }
    public List<Lecon> Lecons { get; set; } = new();
}

public class Lecon : Entity
{
    public string ChapitreId { get; set; } = "";
    public Chapitre Chapitre { get; set; } = null!;
    public string Titre { get; set; } = "";
    public int Ordre { get; set; }
    public int DureeMin { get; set; }

    /// <summary>Copie de travail du contenu (liste de blocs en JSON). Le paquet publié est un instantané distinct.</summary>
    public string ContenuJson { get; set; } = "[]";

    /// <summary>Dernière version PUBLIÉE (ou version en cours de rédaction tant que jamais publiée).</summary>
    public int Version { get; set; } = 1;
    public bool EverPublished { get; set; }
    public PublicationStatus Statut { get; set; } = PublicationStatus.Brouillon;
    public DateTime? PublishedUtc { get; set; }
    public string? NoteVersion { get; set; }

    // Dénormalisé depuis le paquet courant, pour les listes du catalogue
    public string? Checksum { get; set; }
    public long TailleOctets { get; set; }
    public bool HasQuiz { get; set; }
    public int DownloadCount { get; set; }

    /// <summary>Numéro de la prochaine version à publier.</summary>
    public int NextVersion => EverPublished ? Version + 1 : Version;
}

/// <summary>Paquet téléchargeable d'une leçon pour une version donnée (ZIP : lesson.json, quiz.json, images...).</summary>
public class LeconPackage : Entity
{
    public string LeconId { get; set; } = "";
    public int Version { get; set; }
    /// <summary>Chemin relatif au dossier de stockage.</summary>
    public string CheminFichier { get; set; } = "";
    public string Checksum { get; set; } = "";
    public long TailleOctets { get; set; }
    /// <summary>Vrai si le ZIP a été téléversé par l'administrateur (sinon généré depuis ContenuJson).</summary>
    public bool Personnalise { get; set; }
}

public class Quiz : Entity
{
    public string LeconId { get; set; } = "";
    public string Titre { get; set; } = "";
    public string QuestionsJson { get; set; } = "[]";
}

// ---------- Données de l'élève (synchronisables, EF-63) ----------

public abstract class UserOwnedEntity : Entity
{
    public string UserId { get; set; } = "";
    /// <summary>Heure serveur de la dernière écriture ; sert au "pull" incrémental. UpdatedUtc = horloge logique du client (dernière modification gagnante).</summary>
    public DateTime ServerModifiedUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

public class MarquePage : UserOwnedEntity
{
    public string LeconId { get; set; } = "";
    public double Position { get; set; }
    public string? Libelle { get; set; }
}

public class Note : UserOwnedEntity
{
    public string LeconId { get; set; } = "";
    public double Position { get; set; }
    public string Texte { get; set; } = "";
}

public class Favori : UserOwnedEntity
{
    public string LeconId { get; set; } = "";
    public string? Collection { get; set; }
}

public class ProgressionLecon : UserOwnedEntity
{
    public string LeconId { get; set; } = "";
    public ProgressStatus Statut { get; set; }
    public double PourcentageLecture { get; set; }
    public int TempsPasseSec { get; set; }
    public double DernierePosition { get; set; }
}

// ---------- Retours des élèves ----------

public class Signalement : Entity
{
    public string LeconId { get; set; } = "";
    public string? UserId { get; set; }
    public string Type { get; set; } = "autre";
    public string Message { get; set; } = "";
    public ReportState Etat { get; set; } = ReportState.Nouveau;
}

public class Evaluation : Entity
{
    public string UserId { get; set; } = "";
    public string LeconId { get; set; } = "";
    public bool Utile { get; set; }
}
