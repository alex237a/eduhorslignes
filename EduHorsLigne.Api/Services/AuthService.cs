using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Services;

public interface IResetCodeSender
{
    Task SendAsync(string identifiant, string code, CancellationToken ct);
}

/// <summary>Simulation (prévue par le plan de démo) : le code est écrit dans les journaux au lieu d'être envoyé par SMS/e-mail.</summary>
public class LogResetCodeSender(ILogger<LogResetCodeSender> log) : IResetCodeSender
{
    public Task SendAsync(string identifiant, string code, CancellationToken ct)
    {
        log.LogWarning("[SIMULATION SMS/E-MAIL] Code de réinitialisation pour {Identifiant} : {Code}", identifiant, code);
        return Task.CompletedTask;
    }
}

public class AuthService(AppDbContext db, IConfiguration cfg)
{
    public record Result(User? User, int Status, ApiError? Error);

    public async Task<Result> AuthenticateAsync(string? identifiant, string? motDePasse, UserRole role, CancellationToken ct)
    {
        var fields = new Dictionary<string, string>();
        if (!Identifiant.TryNormalize(identifiant, out var id)) fields["identifiant"] = "Identifiant invalide (e-mail ou numéro +237 à 9 chiffres).";
        if (string.IsNullOrEmpty(motDePasse)) fields["motDePasse"] = "Mot de passe requis.";
        if (fields.Count > 0) return new(null, 400, new ApiError("validation", "Données invalides.", fields));

        var max = cfg.GetValue("Auth:MaxFailedAttempts", 5);
        var lockMinutes = cfg.GetValue("Auth:LockoutMinutes", 15);
        var invalid = new ApiError("identifiants_invalides", "Identifiant ou mot de passe incorrect.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Identifiant == id && u.Role == role, ct);
        if (user == null) return new(null, 401, invalid);

        var now = DateTime.UtcNow;
        if (user.LockoutEndUtc is { } end && end > now)
        {
            var wait = (int)Math.Ceiling((end - now).TotalSeconds);
            return new(null, 423, new ApiError("compte_bloque",
                $"Compte temporairement bloqué après {max} échecs. Réessayez dans {Math.Ceiling(wait / 60.0):0} minute(s).",
                RetryAfterSeconds: wait));
        }

        if (!PasswordHelper.Verify(motDePasse!, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= max)
            {
                user.LockoutEndUtc = now.AddMinutes(lockMinutes);
                user.FailedLoginCount = 0;
                await db.SaveChangesAsync(ct);
                return new(null, 423, new ApiError("compte_bloque",
                    $"Compte temporairement bloqué après {max} échecs. Réessayez dans {lockMinutes} minute(s).",
                    RetryAfterSeconds: lockMinutes * 60));
            }
            await db.SaveChangesAsync(ct);
            return new(null, 401, invalid with { TentativesRestantes = max - user.FailedLoginCount });
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await db.SaveChangesAsync(ct);
        return new(user, 200, null);
    }

    public async Task<UserDto> ToDtoAsync(User u, CancellationToken ct)
    {
        string? classeNom = null;
        if (u.ClasseId != null)
            classeNom = await db.Classes.Where(c => c.Id == u.ClasseId).Select(c => c.Nom).FirstOrDefaultAsync(ct);
        return new UserDto(u.Id, u.Prenom, u.Nom, u.Identifiant, u.SousSysteme, u.ClasseId, classeNom, u.Langue, u.Role.ToString());
    }
}
