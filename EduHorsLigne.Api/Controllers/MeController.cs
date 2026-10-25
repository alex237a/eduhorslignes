using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Controllers;

/// <summary>Profil de l'élève connecté (EF-04) et suppression du compte (EF-88).</summary>
[ApiController, Route("api/me"), Authorize]
public class MeController(AppDbContext db, AuthService auth) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user == null ? Error(401, "session_invalide", "Compte introuvable.") : Ok(await auth.ToDtoAsync(user, ct));
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user == null) return Error(401, "session_invalide", "Compte introuvable.");

        var f = new Dictionary<string, string>();
        if (req.Prenom != null && string.IsNullOrWhiteSpace(req.Prenom)) f["prenom"] = "Le prénom ne peut pas être vide.";
        if (req.Nom != null && string.IsNullOrWhiteSpace(req.Nom)) f["nom"] = "Le nom ne peut pas être vide.";
        if (req.Langue != null && !Rules.LangueOk(req.Langue)) f["langue"] = "Langue acceptée : fr ou en.";

        if (req.ClasseId != null)
        {
            var classe = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ClasseId, ct);
            if (classe == null) f["classeId"] = "Classe inconnue.";
            else { user.ClasseId = classe.Id; user.SousSysteme = classe.SousSysteme; }
        }
        if (f.Count > 0) return Error(400, "validation", "Certains champs sont invalides.", f);

        if (req.Prenom != null) user.Prenom = req.Prenom.Trim();
        if (req.Nom != null) user.Nom = req.Nom.Trim();
        if (req.Langue != null) user.Langue = req.Langue;
        user.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(await auth.ToDtoAsync(user, ct));
    }

    /// <summary>Supprime le compte et toutes ses données (notes, marque-pages, favoris, progression, évaluations).</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user == null) return NoContent();
        // Les signalements restent mais sont anonymisés.
        await db.Signalements.Where(s => s.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, (string?)null), ct);
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
