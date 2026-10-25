using System.Text.Json;
using System.Text.Json.Serialization;
using EduHorsLigne.Models;

namespace EduHorsLigne.Services.Api;

/// <summary>Options JSON communes (camelCase, énumérations en texte).</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}

// ----- Contrats de l'API (voir EduHorsLigne.Api/Common/Dtos.cs) -----
public record ApiError(string Code, string Message, Dictionary<string, string>? Fields = null, int? TentativesRestantes = null, int? RetryAfterSeconds = null);
public record ApiUser(string Id, string Prenom, string Nom, string Identifiant, string? SousSysteme, string? ClasseId, string? ClasseNom, string Langue, string Role);
public record ApiAuth(string Token, DateTime ExpiresUtc, ApiUser Utilisateur);
public record ApiClasse(string Id, string Nom, string Cycle, string SousSysteme, int NbMatieres);
public record ApiMatiere(string Id, string ClasseId, string Nom, string Initiale, string Icone, string Ton, int NbChapitres, int NbLecons, long TailleOctets);
public record ApiChapitre(string Id, string MatiereId, string Titre, int Ordre, int NbLecons);
public record ApiLecon(string Id, string ChapitreId, string ChapitreTitre, string MatiereId, string MatiereNom, string Titre, int Ordre,
                       int DureeMin, long TailleOctets, int Version, string? Checksum, DateTime DateMiseAJour, bool AQuiz);
public record ApiChapitreTree(ApiChapitre Chapitre, List<ApiLecon> Lecons);
public record ApiMatiereTree(ApiMatiere Matiere, List<ApiChapitreTree> Chapitres);
public record ApiClasseTree(ApiClasse Classe, List<ApiMatiereTree> Matieres);
public record ApiCatalog(DateTime GenereUtc, List<ApiClasseTree> Classes);
public record ApiResetResponse(string? Message, string? DevCode);

public record ApiSyncFavori(string LeconId, string? Collection, DateTime UpdatedUtc, bool IsDeleted);
public record ApiSyncProgression(string LeconId, string Statut, double PourcentageLecture, int TempsPasseSec, double DernierePosition, DateTime UpdatedUtc);
public record ApiSyncResponse(DateTime ServerTimeUtc, int Appliques, int Ignores, List<ApiSyncFavori>? Favoris, List<ApiSyncProgression>? Progressions);

// ----- Contenu d'un paquet téléchargé (lesson.json / quiz.json) -----
public class LessonFile
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Titre { get; set; } = "";
    public int DureeMin { get; set; }
    public List<string> Sections { get; set; } = new();
    public List<LessonBlock> Blocks { get; set; } = new();
}

public class QuizFile
{
    public string? Titre { get; set; }
    public List<Question> Questions { get; set; } = new();
}
