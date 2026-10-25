using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using EduHorsLigne.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace EduHorsLigne.Api.Controllers;

/// <summary>Détail d'une leçon, téléchargement (unitaire et par lot), mises à jour, signalements et évaluations.</summary>
[ApiController, Route("api")]
public class LessonsController(AppDbContext db, PackageService packages) : ApiControllerBase
{
    private const PublicationStatus Pub = PublicationStatus.Publiee;
    private const int MaxBatch = 200;

    private Task<Lecon?> FindPublishedAsync(string id, CancellationToken ct) =>
        db.Lecons.Include(l => l.Chapitre).ThenInclude(c => c.Matiere)
                 .FirstOrDefaultAsync(l => l.Id == id && l.Statut == Pub, ct);

    private ObjectResult Unavailable() =>
        Error(404, "lecon_indisponible", "Cette leçon n'existe pas, n'est pas publiée ou a été archivée.");

    // ---------- Métadonnées ----------

    [HttpGet("lessons/{id}"), AllowAnonymous]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var l = await FindPublishedAsync(id, ct);
        return l == null ? Unavailable() : Ok(Mapper.ToDto(l, l.Chapitre.Titre, l.Chapitre.MatiereId, l.Chapitre.Matiere.Nom));
    }

    /// <summary>Somme de contrôle SHA-256 du paquet courant (EF-22).</summary>
    [HttpGet("lessons/{id}/checksum"), AllowAnonymous]
    public async Task<IActionResult> Checksum(string id, CancellationToken ct)
    {
        var l = await FindPublishedAsync(id, ct);
        return l == null ? Unavailable() : Ok(new ChecksumDto(l.Id, l.Version, l.Checksum, "SHA-256", l.TailleOctets));
    }

    // ---------- Téléchargement ----------

    /// <summary>
    /// Paquet ZIP d'une leçon. Les requêtes HTTP Range sont gérées : un téléchargement interrompu reprend
    /// là où il s'était arrêté (EF-17). La somme de contrôle est dans l'en-tête X-Checksum-Sha256 et l'ETag.
    /// </summary>
    [HttpGet("lessons/{id}/package"), Authorize]
    public async Task<IActionResult> Package(string id, CancellationToken ct)
    {
        var l = await FindPublishedAsync(id, ct);
        if (l == null) return Unavailable();

        var pkg = await packages.GetCurrentAsync(l, ct);
        var full = pkg == null ? null : packages.FullPath(pkg.CheminFichier);
        if (pkg == null || !System.IO.File.Exists(full))
            return Error(404, "paquet_indisponible", "Le paquet de cette leçon n'est pas encore disponible.");

        // Ne compter qu'un téléchargement par leçon (pas les reprises).
        var range = Request.Headers["Range"].ToString();
        if (string.IsNullOrEmpty(range) || range.StartsWith("bytes=0-"))
            await db.Lecons.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.DownloadCount, x => x.DownloadCount + 1), ct);

        Response.Headers["X-Checksum-Sha256"] = pkg.Checksum;
        Response.Headers["X-Lesson-Version"] = pkg.Version.ToString();
        return PhysicalFile(full!, "application/zip", $"lecon-{l.Id}-v{pkg.Version}.zip", null,
                            new EntityTagHeaderValue($"\"{pkg.Checksum}\""), true);
    }

    [HttpGet("chapters/{id}/package"), Authorize]
    public async Task<IActionResult> ChapterPackage(string id, CancellationToken ct)
    {
        if (!await db.Chapitres.AnyAsync(c => c.Id == id, ct)) return Error(404, "chapitre_introuvable", "Chapitre introuvable.");
        var lecons = await db.Lecons.Where(l => l.ChapitreId == id && l.Statut == Pub).OrderBy(l => l.Ordre).ToListAsync(ct);
        return await BatchAsync(lecons, $"chapitre-{id}.zip", ct);
    }

    [HttpGet("subjects/{id}/package"), Authorize]
    public async Task<IActionResult> SubjectPackage(string id, CancellationToken ct)
    {
        if (!await db.Matieres.AnyAsync(m => m.Id == id, ct)) return Error(404, "matiere_introuvable", "Matière introuvable.");
        var lecons = await db.Lecons.Where(l => l.Chapitre.MatiereId == id && l.Statut == Pub)
                                    .OrderBy(l => l.Chapitre.Ordre).ThenBy(l => l.Ordre).ToListAsync(ct);
        return await BatchAsync(lecons, $"matiere-{id}.zip", ct);
    }

    /// <summary>Sélection libre de leçons (correspond à IDownloadService.DownloadAsync du front).</summary>
    [HttpPost("lessons/package-batch"), Authorize]
    public async Task<IActionResult> PackageBatch(BatchRequest req, CancellationToken ct)
    {
        var ids = (req.LeconIds ?? new()).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
        if (ids.Count == 0) return Error(400, "validation", "Aucune leçon demandée.");
        if (ids.Count > MaxBatch) return Error(400, "trop_de_lecons", $"{MaxBatch} leçons maximum par lot.");

        var found = await db.Lecons.Where(l => ids.Contains(l.Id) && l.Statut == Pub).ToListAsync(ct);
        var ordered = ids.Select(i => found.FirstOrDefault(l => l.Id == i)).Where(l => l != null).Select(l => l!).ToList();
        return await BatchAsync(ordered, "eduhorsligne-lot.zip", ct);
    }

    private async Task<IActionResult> BatchAsync(List<Lecon> lecons, string fileName, CancellationToken ct)
    {
        if (lecons.Count == 0) return Error(404, "aucune_lecon", "Aucune leçon publiée à télécharger.");
        if (lecons.Count > MaxBatch) return Error(400, "trop_de_lecons", $"{MaxBatch} leçons maximum par lot.");

        var (stream, checksum) = await packages.BuildBatchAsync(lecons, ct);
        var ids = lecons.Select(l => l.Id).ToList();
        await db.Lecons.Where(l => ids.Contains(l.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.DownloadCount, x => x.DownloadCount + 1), ct);

        Response.Headers["X-Checksum-Sha256"] = checksum;
        return File(stream, "application/zip", fileName, true);
    }

    // ---------- Mises à jour (EF-23, EF-60) ----------

    /// <summary>
    /// Le téléphone envoie ses leçons et leurs versions ; le serveur répond uniquement pour celles qui ont changé :
    /// « MiseAJour » (version plus récente publiée) ou « Archivee » (RG6 : reste lisible, plus téléchargeable).
    /// Rien n'est jamais supprimé ni modifié côté téléphone sans l'accord de l'élève (EF-62).
    /// </summary>
    [HttpPost("lessons/check-updates"), Authorize]
    public async Task<IActionResult> CheckUpdates(List<ClientLessonVersion> body, CancellationToken ct)
    {
        var client = (body ?? new()).Where(c => !string.IsNullOrWhiteSpace(c.Id)).GroupBy(c => c.Id).Select(g => g.First()).Take(500).ToList();
        var ids = client.Select(c => c.Id).ToList();
        var server = await db.Lecons.AsNoTracking().Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var result = new List<UpdateInfo>();
        foreach (var c in client)
        {
            if (!server.TryGetValue(c.Id, out var l)) continue;
            if (l.Statut == PublicationStatus.Archivee)
                result.Add(new UpdateInfo(l.Id, "Archivee", l.Version, null, 0));
            else if (l.Statut == Pub && l.Version > c.Version)
                result.Add(new UpdateInfo(l.Id, "MiseAJour", l.Version, l.Checksum, l.TailleOctets));
        }
        return Ok(result);
    }

    // ---------- Retours de l'élève (EF-70, EF-71) ----------

    /// <summary>Signalement d'une coquille. L'identifiant généré par l'application rend l'envoi idempotent (file d'attente hors ligne).</summary>
    [HttpPost("lessons/{id}/reports"), Authorize]
    public async Task<IActionResult> Report(string id, ReportRequest req, CancellationToken ct)
    {
        if (!await db.Lecons.AnyAsync(l => l.Id == id, ct)) return Unavailable();
        if (string.IsNullOrWhiteSpace(req.Message) || req.Message.Length > 1000)
            return Error(400, "validation", "Message requis (1000 caractères maximum).", new() { ["message"] = "Message requis (1000 caractères maximum)." });

        if (!string.IsNullOrWhiteSpace(req.Id) && await db.Signalements.AnyAsync(s => s.Id == req.Id, ct))
            return Ok(new { id = req.Id, dejaRecu = true });

        var s = new Signalement
        {
            LeconId = id,
            UserId = UserId,
            Type = string.IsNullOrWhiteSpace(req.Type) ? "autre" : req.Type.Trim()[..Math.Min(50, req.Type.Trim().Length)],
            Message = req.Message.Trim()
        };
        if (!string.IsNullOrWhiteSpace(req.Id) && req.Id.Length <= 64) s.Id = req.Id;
        db.Signalements.Add(s);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { id = s.Id });
    }

    [HttpPost("lessons/{id}/rating"), Authorize]
    public async Task<IActionResult> Rate(string id, RatingRequest req, CancellationToken ct)
    {
        if (!await db.Lecons.AnyAsync(l => l.Id == id, ct)) return Unavailable();
        var uid = UserId;
        var e = await db.Evaluations.FirstOrDefaultAsync(x => x.UserId == uid && x.LeconId == id, ct);
        if (e == null) db.Evaluations.Add(new Evaluation { UserId = uid, LeconId = id, Utile = req.Utile });
        else { e.Utile = req.Utile; e.UpdatedUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
