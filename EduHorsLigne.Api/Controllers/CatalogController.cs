using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Controllers;

/// <summary>
/// Catalogue public (EF-08 à EF-14). Règle RG8 : seules les leçons « publiées » sont visibles ;
/// les chapitres et matières sans leçon publiée sont masqués.
/// </summary>
[ApiController, Route("api"), AllowAnonymous]
public class CatalogController(AppDbContext db) : ApiControllerBase
{
    private const PublicationStatus Pub = PublicationStatus.Publiee;

    [HttpGet("classes")]
    public async Task<ActionResult<List<ClasseDto>>> Classes([FromQuery] SousSysteme? sousSysteme, CancellationToken ct)
    {
        var q = db.Classes.AsNoTracking().AsQueryable();
        if (sousSysteme != null) q = q.Where(c => c.SousSysteme == sousSysteme);
        return await q.OrderBy(c => c.SousSysteme).ThenBy(c => c.Ordre)
                      .Select(c => new ClasseDto(c.Id, c.Nom, c.Cycle, c.SousSysteme, c.Matieres.Count))
                      .ToListAsync(ct);
    }

    [HttpGet("classes/{id}/subjects")]
    public async Task<IActionResult> Subjects(string id, CancellationToken ct)
    {
        if (!await db.Classes.AnyAsync(c => c.Id == id, ct)) return Error(404, "classe_introuvable", "Classe introuvable.");

        var list = await db.Matieres.AsNoTracking().Where(m => m.ClasseId == id)
            .Select(m => new MatiereDto(m.Id, m.ClasseId, m.Nom, m.Initiale, m.Icone, m.Ton,
                m.Chapitres.Count(c => c.Lecons.Any(l => l.Statut == Pub)),
                m.Chapitres.SelectMany(c => c.Lecons).Count(l => l.Statut == Pub),
                m.Chapitres.SelectMany(c => c.Lecons).Where(l => l.Statut == Pub).Sum(l => (long?)l.TailleOctets) ?? 0L))
            .ToListAsync(ct);
        return Ok(list.Where(m => m.NbLecons > 0).OrderBy(m => m.Nom).ToList());
    }

    [HttpGet("subjects/{id}/chapters")]
    public async Task<IActionResult> Chapters(string id, CancellationToken ct)
    {
        if (!await db.Matieres.AnyAsync(m => m.Id == id, ct)) return Error(404, "matiere_introuvable", "Matière introuvable.");

        var list = await db.Chapitres.AsNoTracking().Where(c => c.MatiereId == id).OrderBy(c => c.Ordre)
            .Select(c => new ChapitreDto(c.Id, c.MatiereId, c.Titre, c.Ordre, c.Lecons.Count(l => l.Statut == Pub)))
            .ToListAsync(ct);
        return Ok(list.Where(c => c.NbLecons > 0).ToList());
    }

    [HttpGet("chapters/{id}/lessons")]
    public async Task<IActionResult> Lessons(string id, CancellationToken ct)
    {
        var chap = await db.Chapitres.AsNoTracking().Include(c => c.Matiere).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (chap == null) return Error(404, "chapitre_introuvable", "Chapitre introuvable.");

        var lecons = await db.Lecons.AsNoTracking().Where(l => l.ChapitreId == id && l.Statut == Pub).OrderBy(l => l.Ordre).ToListAsync(ct);
        return Ok(lecons.Select(l => Mapper.ToDto(l, chap.Titre, chap.MatiereId, chap.Matiere.Nom)).ToList());
    }

    /// <summary>
    /// Catalogue complet d'une classe (ou de toutes) en un seul appel : l'application le stocke pour que
    /// l'élève voie, hors ligne, ce qui est disponible même sans avoir téléchargé les leçons (EF-14).
    /// </summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<CatalogDto>> Catalog([FromQuery] string? classeId, CancellationToken ct)
    {
        var q = db.Classes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(classeId)) q = q.Where(c => c.Id == classeId);

        var classes = await q.OrderBy(c => c.SousSysteme).ThenBy(c => c.Ordre)
            .Include(c => c.Matieres.OrderBy(m => m.Nom))
                .ThenInclude(m => m.Chapitres.OrderBy(ch => ch.Ordre))
                    .ThenInclude(ch => ch.Lecons.Where(l => l.Statut == Pub).OrderBy(l => l.Ordre))
            .ToListAsync(ct);

        var tree = new List<ClasseTree>();
        foreach (var c in classes)
        {
            var matieres = new List<MatiereTree>();
            foreach (var m in c.Matieres)
            {
                var chapitres = new List<ChapitreTree>();
                foreach (var ch in m.Chapitres.Where(x => x.Lecons.Count > 0))
                {
                    var lecons = ch.Lecons.Select(l => Mapper.ToDto(l, ch.Titre, m.Id, m.Nom)).ToList();
                    chapitres.Add(new ChapitreTree(new ChapitreDto(ch.Id, m.Id, ch.Titre, ch.Ordre, lecons.Count), lecons));
                }
                if (chapitres.Count == 0) continue;
                var nb = chapitres.Sum(x => x.Lecons.Count);
                var taille = chapitres.Sum(x => x.Lecons.Sum(l => l.TailleOctets));
                matieres.Add(new MatiereTree(new MatiereDto(m.Id, c.Id, m.Nom, m.Initiale, m.Icone, m.Ton, chapitres.Count, nb, taille), chapitres));
            }
            tree.Add(new ClasseTree(new ClasseDto(c.Id, c.Nom, c.Cycle, c.SousSysteme, matieres.Count), matieres));
        }
        return new CatalogDto(DateTime.UtcNow, tree);
    }
}
