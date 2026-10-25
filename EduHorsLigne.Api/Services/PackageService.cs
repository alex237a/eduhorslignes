using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Data;
using EduHorsLigne.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Services;

/// <summary>
/// Construit et retrouve les paquets de leçon (ZIP) : manifest.json + lesson.json (+ quiz.json, + images si paquet téléversé).
/// Le ZIP est déterministe (dates fixes) : même contenu = même somme de contrôle SHA-256 (EF-22).
/// </summary>
public class PackageService(AppDbContext db, IConfiguration cfg, IWebHostEnvironment env)
{
    private static readonly DateTimeOffset FixedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public string RootDir => Path.GetFullPath(cfg["Storage:Path"] ?? "Storage", env.ContentRootPath);
    public static string RelativePath(string leconId, int version) => Path.Combine("packages", leconId, $"v{version}.zip");
    public string FullPath(string relative) => Path.Combine(RootDir, relative);

    /// <summary>Génère le paquet d'une leçon pour la version donnée à partir de son contenu de travail.</summary>
    public async Task<LeconPackage> BuildAsync(Lecon l, int version, CancellationToken ct = default)
    {
        var chap = await db.Chapitres.AsNoTracking().Include(c => c.Matiere).ThenInclude(m => m.Classe)
                                     .FirstAsync(c => c.Id == l.ChapitreId, ct);
        var quiz = await db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.LeconId == l.Id, ct);
        var blocks = Json.ParseBlocks(l.ContenuJson);

        var rel = RelativePath(l.Id, version);
        var full = FullPath(rel);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using (var fs = File.Create(full))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            await WriteJsonAsync(zip, "manifest.json", new
            {
                format = 1,
                lessonId = l.Id,
                version,
                titre = l.Titre,
                chapitre = chap.Titre,
                matiere = chap.Matiere.Nom,
                classe = chap.Matiere.Classe.Nom,
                sousSysteme = chap.Matiere.Classe.SousSysteme,
                dureeMin = l.DureeMin,
                aQuiz = quiz != null
            }, ct);

            await WriteJsonAsync(zip, "lesson.json", new
            {
                id = l.Id,
                version,
                titre = l.Titre,
                dureeMin = l.DureeMin,
                sections = blocks.Where(b => b.Type == BlockType.Heading).Select(b => b.Text).ToList(),
                blocks
            }, ct);

            if (quiz != null)
                await WriteJsonAsync(zip, "quiz.json", new { titre = quiz.Titre, questions = Json.ParseQuestions(quiz.QuestionsJson) }, ct);
        }

        var (sum, size) = await HashFileAsync(full, ct);
        return await UpsertAsync(l.Id, version, rel, sum, size, custom: false, ct);
    }

    /// <summary>Crée ou met à jour la ligne LeconPackage (l'appelant enregistre).</summary>
    public async Task<LeconPackage> UpsertAsync(string leconId, int version, string relativePath, string checksum, long size, bool custom, CancellationToken ct)
    {
        var p = await db.Packages.FirstOrDefaultAsync(x => x.LeconId == leconId && x.Version == version, ct);
        if (p == null)
        {
            p = new LeconPackage { LeconId = leconId, Version = version };
            db.Packages.Add(p);
        }
        p.CheminFichier = relativePath;
        p.Checksum = checksum;
        p.TailleOctets = size;
        p.Personnalise = custom;
        p.UpdatedUtc = DateTime.UtcNow;
        return p;
    }

    public Task<LeconPackage?> GetCurrentAsync(Lecon l, CancellationToken ct) =>
        db.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.LeconId == l.Id && p.Version == l.Version, ct);

    public static async Task<(string Checksum, long Size)> HashFileAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(fs, ct);
        return (Convert.ToHexString(hash).ToLowerInvariant(), fs.Length);
    }

    /// <summary>
    /// Archive de téléchargement par lot (chapitre, matière ou sélection) : un ZIP contenant les ZIP de leçons
    /// et un manifest.json avec la somme de contrôle de chacune. Le flux est un fichier temporaire supprimé à la fermeture.
    /// </summary>
    public async Task<(FileStream Stream, string Checksum)> BuildBatchAsync(IReadOnlyList<Lecon> lecons, CancellationToken ct)
    {
        var ids = lecons.Select(l => l.Id).ToList();
        var pkgs = await db.Packages.AsNoTracking().Where(p => ids.Contains(p.LeconId)).ToListAsync(ct);

        var tmp = Path.Combine(Path.GetTempPath(), $"edu-{Guid.NewGuid():N}.zip");
        var fs = new FileStream(tmp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
        try
        {
            var items = new List<object>();
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var l in lecons)
                {
                    var p = pkgs.FirstOrDefault(x => x.LeconId == l.Id && x.Version == l.Version);
                    if (p == null) continue;
                    var name = $"lessons/{l.Id}.zip";
                    var entry = zip.CreateEntry(name, CompressionLevel.NoCompression); // déjà compressé
                    entry.LastWriteTime = FixedTime;
                    await using (var dst = entry.Open())
                    await using (var src = File.OpenRead(FullPath(p.CheminFichier)))
                        await src.CopyToAsync(dst, ct);
                    items.Add(new { id = l.Id, version = l.Version, titre = l.Titre, checksum = p.Checksum, tailleOctets = p.TailleOctets, fichier = name });
                }
                await WriteJsonAsync(zip, "manifest.json", new { format = 1, items }, ct);
            }

            fs.Position = 0;
            var hash = await SHA256.HashDataAsync(fs, ct);
            fs.Position = 0;
            return (fs, Convert.ToHexString(hash).ToLowerInvariant());
        }
        catch
        {
            await fs.DisposeAsync();
            throw;
        }
    }

    public void DeleteFiles(string leconId)
    {
        var dir = Path.Combine(RootDir, "packages", leconId);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    /// <summary>Vérifie qu'un ZIP téléversé est exploitable : lisible et contenant lesson.json.</summary>
    public static bool IsValidPackage(string path)
    {
        try
        {
            using var zip = ZipFile.OpenRead(path);
            return zip.GetEntry("lesson.json") != null;
        }
        catch (InvalidDataException) { return false; }
    }

    private static async Task WriteJsonAsync(ZipArchive zip, string name, object data, CancellationToken ct)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = FixedTime;
        await using var s = entry.Open();
        await JsonSerializer.SerializeAsync(s, data, Json.Options, ct);
    }
}
