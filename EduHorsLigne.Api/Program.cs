using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ----- Base de données : SQLite (développement). Pour PostgreSQL : package Npgsql.EntityFrameworkCore.PostgreSQL + UseNpgsql. -----
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=eduhorsligne.db"));

// ----- Services -----
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PackageService>();
builder.Services.AddSingleton<IResetCodeSender, LogResetCodeSender>();

// ----- API : JSON camelCase, énumérations en texte, erreurs de validation au format ApiError -----
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var fields = new Dictionary<string, string>();
            foreach (var (key, entry) in ctx.ModelState)
            {
                if (entry.Errors.Count == 0) continue;
                var k = key.TrimStart('$', '.');
                k = k.Length == 0 ? "corps" : char.ToLowerInvariant(k[0]) + k[1..];
                var msg = entry.Errors[0].ErrorMessage;
                fields[k] = string.IsNullOrEmpty(msg) ? "Valeur invalide." : msg;
            }
            return new BadRequestObjectResult(new ApiError("validation", "Requête invalide.", fields));
        };
    });

// Réduit les données mobiles consommées (EF-22, OS5) pour les réponses JSON.
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

// ----- Authentification JWT -----
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key manquant.");
if (jwtKey.Length < 32) throw new InvalidOperationException("Jwt:Key doit contenir au moins 32 caractères.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };
});
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", p => p.RequireRole("Admin"));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("X-Checksum-Sha256", "X-Lesson-Version")));

var app = builder.Build();

// ----- Création de la base + données de démonstration (pour un projet réel : migrations EF Core) -----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<PackageService>(),
                             app.Configuration, app.Logger);
}

app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/admin/"));

app.Run();
