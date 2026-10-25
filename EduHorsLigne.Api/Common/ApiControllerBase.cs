using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EduHorsLigne.Api.Common;

public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Identifiant de l'utilisateur authentifié (claim "sub").</summary>
    protected string UserId => User.FindFirstValue("sub") ?? throw new InvalidOperationException("Utilisateur non authentifié.");

    protected ObjectResult Error(int status, string code, string message, Dictionary<string, string>? fields = null) =>
        StatusCode(status, new ApiError(code, message, fields));

    protected ObjectResult Error(int status, ApiError error) => StatusCode(status, error);

    protected static DateTime AsUtc(DateTime t) =>
        t.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : t.ToUniversalTime();
}
