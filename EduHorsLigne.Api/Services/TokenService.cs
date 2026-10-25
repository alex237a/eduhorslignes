using System.Security.Claims;
using System.Text;
using EduHorsLigne.Api.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EduHorsLigne.Api.Services;

/// <summary>
/// Émet les JWT. Durée longue par défaut (30 jours) : la session doit rester active hors ligne (EF-84).
/// </summary>
public class TokenService(IConfiguration cfg)
{
    public (string Token, DateTime ExpiresUtc) Create(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var expires = DateTime.UtcNow.AddDays(cfg.GetValue("Jwt:ExpiryDays", 30));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id),
                new Claim("role", user.Role.ToString()),
                new Claim("name", $"{user.Prenom} {user.Nom}".Trim())
            }),
            Issuer = cfg["Jwt:Issuer"],
            Audience = cfg["Jwt:Audience"],
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
