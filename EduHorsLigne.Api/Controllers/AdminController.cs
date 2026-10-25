using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using EduHorsLigne.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Controllers;

/// <summary>
/// Administration du contenu (EF-72 à EF-78, CU5). Un seul rôle « Admin » pour la première démo.
/// RG9 : aucun endpoint d'administration n'expose les notes, favoris ou marque-pages des élèves.
/// </summary>
[ApiController, Route("api/admin"), Authorize(Policy = "Admin")]
public class AdminController(AppDbContext db, PackageService packages, AuthService auth, TokenService tokens) : ApiControllerBase
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    // =============== Authentification ===============

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var r = await auth.AuthenticateAsync(req.Identifiant, req.MotDePasse, UserRole.Admin, ct);
        if (r.User == null) return Error(r.Status, r.Error!);
        var (token, exp) = tokens.Create(r.User);
        return Ok(new AuthResponse(token, exp, await auth.ToDtoAsync(r.User, ct)));
    }

    // =============== Classes ===============

    [HttpGet("classes")]
    public async Task<List<ClasseDto>> Classes(CancellationToken ct) =>
        await db.Classes.AsNoTracking().OrderBy(c => c.SousSysteme).ThenBy(c => c.Ordre)
                .Select(c => new ClasseDto(c.Id, c.Nom, c.Cycle, c.SousSysteme, c.Matieres.Count)).ToListAsync(ct);

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClasse(ClasseUpsert r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Nom) || string.IsNullOrWhiteSpace(r.Cycle) || r.SousSysteme == null)
            return Error(400, "validation", "Nom, cycle et sous-système sont obligatoires.");
        var c = new Classe { Nom = r.Nom.Trim(), Cycle = r.Cycle.Trim(), SousSysteme = r.SousSysteme.Value, Ordre = r.Ordre ?? 0 };
        db.Classes.Add(c);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new ClasseDto(c.Id, c.Nom, c.Cycle, c.SousSysteme, 0));
    }

    [HttpPut("classes/{id}")]
    public async Task<IActionResult> UpdateClasse(string id, ClasseUpsert r, CancellationToken ct)
    {
        var c = await db.Classes.FindAsync(new object[] { id }, ct);
        if (c == null) return Error(404, "classe_introuvable", "Classe introuvable.");
        if (r.Nom != null) c.Nom = r.Nom.Trim();
        if (r.Cycle != null) c.Cycle = r.Cycle.Trim();
        if (r.SousSysteme != null) c.SousSysteme = r.SousSysteme.Value;
        if (r.Ordre != null) c.Ordre = r.Ordre.Value;
        c.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("classes/{id}")]
    public async Task<IActionResult> DeleteClasse(string id, CancellationToken ct)
    {
        var c = await db.Classes.FindAsync(new object[] { id }, ct);
        if (c == null) return NoContent();
        if (await db.Matieres.AnyAsync(m => m.ClasseId == id, ct)) return Error(409, "non_vide", "Supprimez d'abord les matières de cette classe.");
        db.Classes.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =============== Matières ===============

    [HttpGet("subjects")]
    public async Task<List<AdminMatiereDto>> Subjects([FromQuery] string? classeId, CancellationToken ct)
    {
        var q = db.Matieres.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(classeId)) q = q.Where(m => m.ClasseId == classeId);
        return await q.OrderBy(m => m.Classe.Ordre).ThenBy(m => m.Nom)
                      .Select(m => new AdminMatiereDto(m.Id, m.ClasseId, m.Classe.Nom, m.Nom, m.Initiale, m.Icone, m.Ton)).ToListAsync(ct);
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(MatiereUpsert r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Nom) || string.IsNullOrWhiteSpace(r.ClasseId)) return Error(400, "validation", "Nom et classe sont obligatoires.");
        if (!await db.Classes.AnyAsync(c => c.Id == r.ClasseId, ct)) return Error(400, "validation", "Classe inconnue.");
        var nom = r.Nom.Trim();
        var m = new Matiere
        {
            ClasseId = r.ClasseId, Nom = nom,
            Initiale = string.IsNullOrWhiteSpace(r.Initiale) ? nom[..1].ToUpperInvariant() : r.Initiale.Trim(),
            Icone = r.Icone?.Trim() ?? "", Ton = r.Ton ?? "Neutral"
        };
        db.Matieres.Add(m);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { m.Id });
    }

    [HttpPut("subjects/{id}")]
    public async Task<IActionResult> UpdateSubject(string id, MatiereUpsert r, CancellationToken ct)
    {
        var m = await db.Matieres.FindAsync(new object[] { id }, ct);
        if (m == null) return Error(404, "matiere_introuvable", "Matière introuvable.");
        if (r.ClasseId != null)
        {
            if (!await db.Classes.AnyAsync(c => c.Id == r.ClasseId, ct)) return Error(400, "validation", "Classe inconnue.");
            m.ClasseId = r.ClasseId;
        }
        if (r.Nom != null) m.Nom = r.Nom.Trim();
        if (r.Initiale != null) m.Initiale = r.Initiale.Trim();
        if (r.Icone != null) m.Icone = r.Icone.Trim();
        if (r.Ton != null) m.Ton = r.Ton;
        m.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("subjects/{id}")]
    public async Task<IActionResult> DeleteSubject(string id, CancellationToken ct)
    {
        var m = await db.Matieres.FindAsync(new object[] { id }, ct);
        if (m == null) return NoContent();
        if (await db.Chapitres.AnyAsync(c => c.MatiereId == id, ct)) return Error(409, "non_vide", "Supprimez d'abord les chapitres de cette matière.");
        db.Matieres.Remove(m);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =============== Chapitres ===============

    [HttpGet("chapters")]
    public async Task<List<AdminChapitreDto>> Chapters([FromQuery] string? matiereId, CancellationToken ct)
    {
        var q = db.Chapitres.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(matiereId)) q = q.Where(c => c.MatiereId == matiereId);
        return await q.OrderBy(c => c.Matiere.Classe.Ordre).ThenBy(c => c.Matiere.Nom).ThenBy(c => c.Ordre)
                      .Select(c => new AdminChapitreDto(c.Id, c.MatiereId, c.Matiere.Nom, c.Matiere.Classe.Nom, c.Titre, c.Ordre)).ToListAsync(ct);
    }

    [HttpPost("chapters")]
    public async Task<IActionResult> CreateChapter(ChapitreUpsert r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Titre) || string.IsNullOrWhiteSpace(r.MatiereId)) return Error(400, "validation", "Titre et matière sont obligatoires.");
        if (!await db.Matieres.AnyAsync(m => m.Id == r.MatiereId, ct)) return Error(400, "validation", "Matière inconnue.");
        var ordre = r.Ordre ?? (await db.Chapitres.Where(c => c.MatiereId == r.MatiereId).MaxAsync(c => (int?)c.Ordre, ct) ?? 0) + 1;
        var c = new Chapitre { MatiereId = r.MatiereId, Titre = r.Titre.Trim(), Ordre = ordre };
        db.Chapitres.Add(c);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { c.Id, c.Ordre });
    }

    [HttpPut("chapters/{id}")]
    public async Task<IActionResult> UpdateChapter(string id, ChapitreUpsert r, CancellationToken ct)
    {
        var c = await db.Chapitres.FindAsync(new object[] { id }, ct);
        if (c == null) return Error(404, "chapitre_introuvable", "Chapitre introuvable.");
        if (r.MatiereId != null)
        {
            if (!await db.Matieres.AnyAsync(m => m.Id == r.MatiereId, ct)) return Error(400, "validation", "Matière inconnue.");
            c.MatiereId = r.MatiereId;
        }
        if (r.Titre != null) c.Titre = r.Titre.Trim();
        if (r.Ordre != null) c.Ordre = r.Ordre.Value;
        c.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("chapters/{id}")]
    public async Task<IActionResult> DeleteChapter(string id, CancellationToken ct)
    {
        var c = await db.Chapitres.FindAsync(new object[] { id }, ct);
        if (c == null) return NoContent();
        if (await db.Lecons.AnyAsync(l => l.ChapitreId == id, ct)) return Error(409, "non_vide", "Supprimez ou archivez d'abord les leçons de ce chapitre.");
        db.Chapitres.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =============== Leçons ===============

    [HttpGet("lessons")]
    public async Task<List<AdminLeconDto>> Lessons([FromQuery] string? chapitreId, [FromQuery] PublicationStatus? statut, CancellationToken ct)
    {
        var q = LessonsQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(chapitreId)) q = q.Where(l => l.ChapitreId == chapitreId);
        if (statut != null) q = q.Where(l => l.Statut == statut);
        var list = await q.OrderBy(l => l.Chapitre.Matiere.Classe.Ordre).ThenBy(l => l.Chapitre.Matiere.Nom)
                          .ThenBy(l => l.Chapitre.Ordre).ThenBy(l => l.Ordre).ToListAsync(ct);
        return list.Select(l => ToAdminDto(l, detail: false, null)).ToList();
    }

    [HttpGet("lessons/{id}")]
    public async Task<IActionResult> GetLesson(string id, CancellationToken ct)
    {
        var l = await LessonsQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        var quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.LeconId == id, ct);
        QuizDto? quizDto = quiz == null ? null : new QuizDto(quiz.Titre, Json.ParseQuestions(quiz.QuestionsJson));
        return Ok(ToAdminDto(l, detail: true, quizDto));
    }

    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson(LeconUpsert r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Titre) || r.Titre.Length > 200) return Error(400, "validation", "Titre requis (200 caractères maximum).");
        if (string.IsNullOrWhiteSpace(r.ChapitreId) || !await db.Chapitres.AnyAsync(c => c.Id == r.ChapitreId, ct))
            return Error(400, "validation", "Chapitre inconnu.");
        var blockError = Rules.ValidateBlocks(r.Blocs);
        if (blockError != null) return Error(400, "contenu_invalide", blockError);

        var ordre = r.Ordre ?? (await db.Lecons.Where(l => l.ChapitreId == r.ChapitreId).MaxAsync(l => (int?)l.Ordre, ct) ?? 0) + 1;
        var l = new Lecon
        {
            ChapitreId = r.ChapitreId, Titre = r.Titre.Trim(), Ordre = ordre, DureeMin = Math.Max(0, r.DureeMin ?? 0),
            ContenuJson = Json.ToJson(r.Blocs ?? new()), Statut = PublicationStatus.Brouillon, Version = 1
        };
        db.Lecons.Add(l);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, new { l.Id, l.Statut, l.Version });
    }

    /// <summary>
    /// Modifie la copie de travail. Si la leçon est déjà publiée, les élèves continuent de recevoir la version publiée
    /// jusqu'à la prochaine publication (CU5).
    /// </summary>
    [HttpPut("lessons/{id}")]
    public async Task<IActionResult> UpdateLesson(string id, LeconUpsert r, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        var blockError = Rules.ValidateBlocks(r.Blocs);
        if (blockError != null) return Error(400, "contenu_invalide", blockError);

        if (r.ChapitreId != null)
        {
            if (!await db.Chapitres.AnyAsync(c => c.Id == r.ChapitreId, ct)) return Error(400, "validation", "Chapitre inconnu.");
            l.ChapitreId = r.ChapitreId;
        }
        if (r.Titre != null)
        {
            if (string.IsNullOrWhiteSpace(r.Titre) || r.Titre.Length > 200) return Error(400, "validation", "Titre invalide.");
            l.Titre = r.Titre.Trim();
        }
        if (r.Ordre != null) l.Ordre = r.Ordre.Value;
        if (r.DureeMin != null) l.DureeMin = Math.Max(0, r.DureeMin.Value);
        if (r.Blocs != null) l.ContenuJson = Json.ToJson(r.Blocs);
        l.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Suppression définitive : uniquement pour une leçon jamais publiée. Sinon, l'archiver (RG6).</summary>
    [HttpDelete("lessons/{id}")]
    public async Task<IActionResult> DeleteLesson(string id, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return NoContent();
        if (l.EverPublished) return Error(409, "deja_publiee", "Cette leçon a déjà été publiée : archivez-la plutôt que de la supprimer.");
        db.Lecons.Remove(l);
        await db.SaveChangesAsync(ct);
        packages.DeleteFiles(id);
        return NoContent();
    }

    // ----- Cycle de vie : brouillon → en relecture → publiée → archivée (EF-75) -----

    [HttpPost("lessons/{id}/submit-review")]
    public async Task<IActionResult> SubmitReview(string id, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        if (l.Statut != PublicationStatus.Brouillon) return Error(409, "transition_invalide", "Seul un brouillon peut être envoyé en relecture.");
        l.Statut = PublicationStatus.EnRelecture;
        l.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { l.Id, l.Statut });
    }

    /// <summary>
    /// Publie la leçon (CU5). La première publication garde la version 1 ; chaque republication incrémente le numéro de version,
    /// ce que l'application détecte via /api/lessons/check-updates. Un paquet téléversé pour cette version est utilisé tel quel,
    /// sinon le paquet est généré depuis le contenu de travail.
    /// </summary>
    [HttpPost("lessons/{id}/publish")]
    public async Task<IActionResult> Publish(string id, PublishRequest? req, CancellationToken ct)
    {
        var l = await db.Lecons.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");

        var target = l.NextVersion;
        var custom = await db.Packages.FirstOrDefaultAsync(p => p.LeconId == id && p.Version == target && p.Personnalise, ct);
        if (custom == null && Json.ParseBlocks(l.ContenuJson).Count == 0)
            return Error(422, "contenu_vide", "Ajoutez du contenu (ou téléversez un paquet) avant de publier.");

        var pkg = custom ?? await packages.BuildAsync(l, target, ct);
        l.Version = target;
        l.Checksum = pkg.Checksum;
        l.TailleOctets = pkg.TailleOctets;
        l.Statut = PublicationStatus.Publiee;
        l.EverPublished = true;
        l.PublishedUtc = DateTime.UtcNow;
        l.NoteVersion = req?.NoteVersion;
        l.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { l.Id, l.Version, l.Statut, l.Checksum, l.TailleOctets });
    }

    /// <summary>Retire la leçon du catalogue sans l'archiver (retour en brouillon).</summary>
    [HttpPost("lessons/{id}/unpublish")]
    public async Task<IActionResult> Unpublish(string id, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        if (l.Statut != PublicationStatus.Publiee) return Error(409, "transition_invalide", "La leçon n'est pas publiée.");
        l.Statut = PublicationStatus.Brouillon;
        l.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { l.Id, l.Statut });
    }

    /// <summary>RG6 : une leçon archivée n'est plus téléchargeable mais reste lisible chez les élèves qui l'ont déjà.</summary>
    [HttpPost("lessons/{id}/archive")]
    public async Task<IActionResult> Archive(string id, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        if (l.Statut == PublicationStatus.Archivee) return Ok(new { l.Id, l.Statut });
        l.Statut = PublicationStatus.Archivee;
        l.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { l.Id, l.Statut });
    }

    // ----- Paquet et quiz -----

    /// <summary>Téléverse un paquet ZIP prêt à l'emploi (doit contenir lesson.json ; images et quiz.json facultatifs).</summary>
    [HttpPost("lessons/{id}/package"), RequestSizeLimit(MaxUploadBytes), Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPackage(string id, IFormFile? file, CancellationToken ct)
    {
        var l = await db.Lecons.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        if (file == null || file.Length == 0) return Error(400, "validation", "Fichier ZIP requis (champ « file »).");
        if (file.Length > MaxUploadBytes) return Error(413, "fichier_trop_gros", "25 Mo maximum.");

        var target = l.NextVersion;
        var rel = PackageService.RelativePath(id, target);
        var full = packages.FullPath(rel);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        var tmp = full + ".upload";
        await using (var fs = System.IO.File.Create(tmp)) await file.CopyToAsync(fs, ct);
        if (!PackageService.IsValidPackage(tmp))
        {
            System.IO.File.Delete(tmp);
            return Error(400, "paquet_invalide", "ZIP illisible ou lesson.json manquant.");
        }
        System.IO.File.Move(tmp, full, overwrite: true);

        var (sum, size) = await PackageService.HashFileAsync(full, ct);
        await packages.UpsertAsync(id, target, rel, sum, size, custom: true, ct);
        await db.SaveChangesAsync(ct);
        return Ok(new { version = target, checksum = sum, tailleOctets = size,
                        message = "Paquet enregistré : il sera utilisé à la prochaine publication." });
    }

    [HttpPut("lessons/{id}/quiz")]
    public async Task<IActionResult> SetQuiz(string id, QuizDto body, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return Error(404, "lecon_introuvable", "Leçon introuvable.");
        var err = Rules.ValidateQuiz(body);
        if (err != null) return Error(400, "quiz_invalide", err);

        var q = await db.Quizzes.FirstOrDefaultAsync(x => x.LeconId == id, ct);
        if (q == null) { q = new Quiz { LeconId = id }; db.Quizzes.Add(q); }
        q.Titre = string.IsNullOrWhiteSpace(body.Titre) ? $"Quiz : {l.Titre}" : body.Titre.Trim();
        q.QuestionsJson = Json.ToJson(body.Questions);
        q.UpdatedUtc = DateTime.UtcNow;
        l.HasQuiz = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("lessons/{id}/quiz")]
    public async Task<IActionResult> DeleteQuiz(string id, CancellationToken ct)
    {
        var l = await db.Lecons.FindAsync(new object[] { id }, ct);
        if (l == null) return NoContent();
        await db.Quizzes.Where(q => q.LeconId == id).ExecuteDeleteAsync(ct);
        l.HasQuiz = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =============== Statistiques anonymes et signalements (EF-78) ===============

    [HttpGet("stats")]
    public async Task<StatsDto> Stats(CancellationToken ct)
    {
        var eleves = await db.Users.CountAsync(u => u.Role == UserRole.Eleve, ct);
        var publiees = await db.Lecons.CountAsync(l => l.Statut == PublicationStatus.Publiee, ct);
        var total = await db.Lecons.SumAsync(l => (int?)l.DownloadCount, ct) ?? 0;
        var top = await db.Lecons.AsNoTracking().Where(l => l.DownloadCount > 0).OrderByDescending(l => l.DownloadCount).Take(5)
                                 .Select(l => new TopLecon(l.Id, l.Titre, l.DownloadCount)).ToListAsync(ct);

        var progresses = await db.Progressions.CountAsync(ct);
        var done = await db.Progressions.CountAsync(p => p.Statut == ProgressStatus.Terminee, ct);
        var taux = progresses == 0 ? 0 : Math.Round(done * 100.0 / progresses, 1);

        var nouveaux = await db.Signalements.CountAsync(s => s.Etat == ReportState.Nouveau, ct);
        var totalReports = await db.Signalements.CountAsync(ct);
        return new StatsDto(eleves, publiees, total, taux, nouveaux, totalReports, top);
    }

    [HttpGet("reports")]
    public async Task<List<SignalementDto>> Reports([FromQuery] ReportState? etat, CancellationToken ct)
    {
        var q = db.Signalements.AsNoTracking().AsQueryable();
        if (etat != null) q = q.Where(s => s.Etat == etat);
        var list = await q.OrderByDescending(s => s.CreatedUtc).Take(200).ToListAsync(ct);
        var leconIds = list.Select(s => s.LeconId).Distinct().ToList();
        var titres = await db.Lecons.AsNoTracking().Where(l => leconIds.Contains(l.Id))
                             .ToDictionaryAsync(l => l.Id, l => l.Titre, ct);
        return list.Select(s => new SignalementDto(s.Id, s.LeconId, titres.GetValueOrDefault(s.LeconId), s.Type, s.Message, s.Etat, s.CreatedUtc)).ToList();
    }

    [HttpPost("reports/{id}/resolve")]
    public async Task<IActionResult> ResolveReport(string id, CancellationToken ct)
    {
        var s = await db.Signalements.FindAsync(new object[] { id }, ct);
        if (s == null) return Error(404, "signalement_introuvable", "Signalement introuvable.");
        s.Etat = ReportState.Traite;
        s.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // =============== Helpers ===============

    private IQueryable<Lecon> LessonsQuery() =>
        db.Lecons.Include(l => l.Chapitre).ThenInclude(c => c.Matiere).ThenInclude(m => m.Classe);

    private static AdminLeconDto ToAdminDto(Lecon l, bool detail, QuizDto? quiz) =>
        new(l.Id, l.ChapitreId, l.Chapitre.Titre, l.Chapitre.Matiere.Nom, l.Chapitre.Matiere.Classe.Nom,
            l.Titre, l.Ordre, l.DureeMin, l.Statut, l.Version, l.NextVersion, l.Checksum, l.TailleOctets,
            l.DownloadCount, l.PublishedUtc, l.UpdatedUtc, l.HasQuiz,
            detail ? Json.ParseBlocks(l.ContenuJson) : null, quiz);
}
