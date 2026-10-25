using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Controllers;

/// <summary>
/// Synchronisation minimale (EF-63) : le téléphone envoie ses modifications locales depuis la dernière synchro
/// et reçoit celles du serveur (autres appareils). Résolution des conflits : dernière modification gagnante
/// (comparaison des horloges UpdatedUtc ; en cas d'égalité, le serveur garde sa version).
/// Les identifiants de notes et marque-pages sont générés par l'application (GUID) pour pouvoir créer hors ligne.
/// Favoris et progression ont pour clé naturelle (élève, leçon). Les suppressions sont logiques (IsDeleted).
/// </summary>
[ApiController, Route("api/sync"), Authorize]
public class SyncController(AppDbContext db) : ApiControllerBase
{
    private const int MaxItemsPerType = 1000;

    [HttpPost]
    public async Task<IActionResult> Sync(SyncRequest req, CancellationToken ct)
    {
        var uid = UserId;
        var now = DateTime.UtcNow;
        var since = req.SinceUtc is { } s ? AsUtc(s) : DateTime.MinValue;
        int applied = 0, ignored = 0;

        if ((req.MarquePages?.Count ?? 0) > MaxItemsPerType || (req.Notes?.Count ?? 0) > MaxItemsPerType ||
            (req.Favoris?.Count ?? 0) > MaxItemsPerType || (req.Progressions?.Count ?? 0) > MaxItemsPerType)
            return Error(400, "trop_d_elements", $"{MaxItemsPerType} éléments maximum par type et par synchronisation.");

        // ----- Marque-pages -----
        foreach (var d in Latest(req.MarquePages, x => x.Id, x => x.UpdatedUtc))
        {
            if (string.IsNullOrWhiteSpace(d.Id) || d.Id.Length > 64 || string.IsNullOrWhiteSpace(d.LeconId) || (d.Libelle?.Length ?? 0) > 200) { ignored++; continue; }
            var t = Clock(d.UpdatedUtc, now);
            var e = await db.MarquePages.FirstOrDefaultAsync(x => x.Id == d.Id, ct);
            if (e != null && e.UserId != uid) { ignored++; continue; }
            if (e == null) { e = new MarquePage { Id = d.Id, UserId = uid, CreatedUtc = t }; db.MarquePages.Add(e); }
            else if (t <= e.UpdatedUtc) { ignored++; continue; }
            e.LeconId = d.LeconId; e.Position = d.Position; e.Libelle = d.Libelle; e.IsDeleted = d.IsDeleted;
            e.UpdatedUtc = t; e.ServerModifiedUtc = now;
            applied++;
        }

        // ----- Notes -----
        foreach (var d in Latest(req.Notes, x => x.Id, x => x.UpdatedUtc))
        {
            if (string.IsNullOrWhiteSpace(d.Id) || d.Id.Length > 64 || string.IsNullOrWhiteSpace(d.LeconId) || (d.Texte?.Length ?? 0) > 10000) { ignored++; continue; }
            var t = Clock(d.UpdatedUtc, now);
            var e = await db.Notes.FirstOrDefaultAsync(x => x.Id == d.Id, ct);
            if (e != null && e.UserId != uid) { ignored++; continue; }
            if (e == null) { e = new Note { Id = d.Id, UserId = uid, CreatedUtc = t }; db.Notes.Add(e); }
            else if (t <= e.UpdatedUtc) { ignored++; continue; }
            e.LeconId = d.LeconId; e.Position = d.Position; e.Texte = d.Texte ?? ""; e.IsDeleted = d.IsDeleted;
            e.UpdatedUtc = t; e.ServerModifiedUtc = now;
            applied++;
        }

        // ----- Favoris (clé : élève + leçon) -----
        foreach (var d in Latest(req.Favoris, x => x.LeconId, x => x.UpdatedUtc))
        {
            if (string.IsNullOrWhiteSpace(d.LeconId) || (d.Collection?.Length ?? 0) > 100) { ignored++; continue; }
            var t = Clock(d.UpdatedUtc, now);
            var e = await db.Favoris.FirstOrDefaultAsync(x => x.UserId == uid && x.LeconId == d.LeconId, ct);
            if (e == null) { e = new Favori { UserId = uid, LeconId = d.LeconId, CreatedUtc = t }; db.Favoris.Add(e); }
            else if (t <= e.UpdatedUtc) { ignored++; continue; }
            e.Collection = d.Collection; e.IsDeleted = d.IsDeleted;
            e.UpdatedUtc = t; e.ServerModifiedUtc = now;
            applied++;
        }

        // ----- Progression (clé : élève + leçon) -----
        foreach (var d in Latest(req.Progressions, x => x.LeconId, x => x.UpdatedUtc))
        {
            if (string.IsNullOrWhiteSpace(d.LeconId)) { ignored++; continue; }
            var t = Clock(d.UpdatedUtc, now);
            var e = await db.Progressions.FirstOrDefaultAsync(x => x.UserId == uid && x.LeconId == d.LeconId, ct);
            if (e == null) { e = new ProgressionLecon { UserId = uid, LeconId = d.LeconId, CreatedUtc = t }; db.Progressions.Add(e); }
            else if (t <= e.UpdatedUtc) { ignored++; continue; }
            e.Statut = d.Statut;
            e.PourcentageLecture = Math.Clamp(d.PourcentageLecture, 0, 1);
            e.TempsPasseSec = Math.Max(0, d.TempsPasseSec);
            e.DernierePosition = d.DernierePosition;
            e.UpdatedUtc = t; e.ServerModifiedUtc = now;
            applied++;
        }

        await db.SaveChangesAsync(ct);

        // ----- Pull : ce qui a changé côté serveur depuis la dernière synchro -----
        var marques = await db.MarquePages.AsNoTracking().Where(x => x.UserId == uid && x.ServerModifiedUtc > since).ToListAsync(ct);
        var notes = await db.Notes.AsNoTracking().Where(x => x.UserId == uid && x.ServerModifiedUtc > since).ToListAsync(ct);
        var favoris = await db.Favoris.AsNoTracking().Where(x => x.UserId == uid && x.ServerModifiedUtc > since).ToListAsync(ct);
        var progs = await db.Progressions.AsNoTracking().Where(x => x.UserId == uid && x.ServerModifiedUtc > since).ToListAsync(ct);

        return Ok(new SyncResponse(now, applied, ignored,
            marques.Select(x => new SyncMarquePage(x.Id, x.LeconId, x.Position, x.Libelle, x.UpdatedUtc, x.IsDeleted)).ToList(),
            notes.Select(x => new SyncNote(x.Id, x.LeconId, x.Position, x.Texte, x.UpdatedUtc, x.IsDeleted)).ToList(),
            favoris.Select(x => new SyncFavori(x.LeconId, x.Collection, x.UpdatedUtc, x.IsDeleted)).ToList(),
            progs.Select(x => new SyncProgression(x.LeconId, x.Statut, x.PourcentageLecture, x.TempsPasseSec, x.DernierePosition, x.UpdatedUtc)).ToList()));
    }

    /// <summary>Si une même clé apparaît plusieurs fois dans la requête, seule l'entrée la plus récente est conservée.</summary>
    private static List<T> Latest<T>(List<T>? items, Func<T, string?> key, Func<T, DateTime> time) =>
        (items ?? new()).GroupBy(x => key(x) ?? "").Select(g => g.OrderByDescending(time).First()).ToList();

    /// <summary>Horloge du client en UTC ; plafonnée à « maintenant + 5 min » pour qu'un téléphone mal réglé ne gagne pas tous les conflits.</summary>
    private static DateTime Clock(DateTime clientTime, DateTime now)
    {
        var t = AsUtc(clientTime);
        return t > now.AddMinutes(5) ? now : t;
    }
}
