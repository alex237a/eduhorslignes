using System.Security.Cryptography;
using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using EduHorsLigne.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Controllers;

/// <summary>Authentification élève : inscription, connexion, récupération du mot de passe (EF-79 à EF-86).</summary>
[ApiController, Route("api/auth"), AllowAnonymous]
public class AuthController(AppDbContext db, AuthService auth, TokenService tokens, IResetCodeSender sender,
                            IConfiguration cfg) : ApiControllerBase
{
    private const int MaxCodeAttempts = 5;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var f = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(req.Nom)) f["nom"] = "Le nom est obligatoire.";
        if (string.IsNullOrWhiteSpace(req.Prenom)) f["prenom"] = "Le prénom est obligatoire.";
        if (!Identifiant.TryNormalize(req.Identifiant, out var id)) f["identifiant"] = "E-mail ou numéro camerounais (+237 et 9 chiffres) invalide.";
        if (!Rules.PasswordOk(req.MotDePasse)) f["motDePasse"] = $"Au moins {Rules.MinPasswordLength} caractères.";
        if (req.MotDePasse != req.ConfirmationMotDePasse) f["confirmationMotDePasse"] = "Les mots de passe ne correspondent pas.";
        if (!req.AccepteConditions) f["accepteConditions"] = "Vous devez accepter les conditions d'utilisation.";
        if (req.Langue != null && !Rules.LangueOk(req.Langue)) f["langue"] = "Langue acceptée : fr ou en.";

        Classe? classe = null;
        if (string.IsNullOrWhiteSpace(req.ClasseId)) f["classeId"] = "Choisissez votre classe.";
        else
        {
            classe = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ClasseId, ct);
            if (classe == null) f["classeId"] = "Classe inconnue.";
        }
        if (f.Count > 0) return Error(400, "validation", "Certains champs sont invalides.", f);

        if (await db.Users.AnyAsync(u => u.Identifiant == id, ct))
            return Error(409, "identifiant_deja_utilise", "Un compte existe déjà avec cet identifiant.",
                         new() { ["identifiant"] = "Identifiant déjà utilisé." });

        var user = new User
        {
            Nom = req.Nom!.Trim(),
            Prenom = req.Prenom!.Trim(),
            Identifiant = id,
            PasswordHash = PasswordHelper.Hash(req.MotDePasse!),
            Role = UserRole.Eleve,
            ClasseId = classe!.Id,
            SousSysteme = classe.SousSysteme,
            Langue = req.Langue ?? "fr",
            ConsentementUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return StatusCode(201, await BuildAuthAsync(user, ct));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var r = await auth.AuthenticateAsync(req.Identifiant, req.MotDePasse, UserRole.Eleve, ct);
        if (r.User == null) return Error(r.Status, r.Error!);
        return Ok(await BuildAuthAsync(r.User, ct));
    }

    // ---- Récupération du mot de passe (code simulé : voir IResetCodeSender) ----

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        if (!Identifiant.TryNormalize(req.Identifiant, out var id))
            return Error(400, "validation", "Identifiant invalide.", new() { ["identifiant"] = "E-mail ou numéro +237 invalide." });

        string? devCode = null;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == id && u.Role == UserRole.Eleve, ct);
        if (user != null)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            user.ResetCodeHash = PasswordHelper.HashCode(code, user.Id);
            user.ResetCodeExpiresUtc = DateTime.UtcNow.AddMinutes(10);
            user.ResetCodeAttempts = 0;
            await db.SaveChangesAsync(ct);
            await sender.SendAsync(user.Identifiant, code, ct);
            if (cfg.GetValue("Auth:ExposeResetCode", false)) devCode = code;
        }
        // Réponse identique que le compte existe ou non (pas d'énumération de comptes).
        return Ok(new { message = "Si ce compte existe, un code de vérification a été envoyé.", devCode });
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode(VerifyCodeRequest req, CancellationToken ct)
    {
        var (user, err) = await CheckCodeAsync(req.Identifiant, req.Code, consume: false, ct);
        return user == null ? Error(400, err!) : Ok(new { valide = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        if (!Rules.PasswordOk(req.NouveauMotDePasse))
            return Error(400, "validation", "Mot de passe trop court.", new() { ["nouveauMotDePasse"] = $"Au moins {Rules.MinPasswordLength} caractères." });

        var (user, err) = await CheckCodeAsync(req.Identifiant, req.Code, consume: true, ct);
        if (user == null) return Error(400, err!);

        user.PasswordHash = PasswordHelper.Hash(req.NouveauMotDePasse!);
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Mot de passe modifié. Vous pouvez vous connecter." });
    }

    private async Task<(User? User, ApiError? Error)> CheckCodeAsync(string? identifiant, string? code, bool consume, CancellationToken ct)
    {
        var bad = new ApiError("code_invalide", "Code invalide ou expiré.");
        if (!Identifiant.TryNormalize(identifiant, out var id) || string.IsNullOrWhiteSpace(code)) return (null, bad);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == id && u.Role == UserRole.Eleve, ct);
        if (user?.ResetCodeHash == null || user.ResetCodeExpiresUtc < DateTime.UtcNow || user.ResetCodeAttempts >= MaxCodeAttempts)
            return (null, bad);

        if (PasswordHelper.HashCode(code.Trim(), user.Id) != user.ResetCodeHash)
        {
            user.ResetCodeAttempts++;
            await db.SaveChangesAsync(ct);
            return (null, bad);
        }

        if (consume)
        {
            user.ResetCodeHash = null;
            user.ResetCodeExpiresUtc = null;
            user.ResetCodeAttempts = 0;
        }
        return (user, null);
    }

    private async Task<AuthResponse> BuildAuthAsync(User user, CancellationToken ct)
    {
        var (token, exp) = tokens.Create(user);
        return new AuthResponse(token, exp, await auth.ToDtoAsync(user, ct));
    }
}
