using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.Services.Interfaces;

namespace EduHorsLigne.Services.Api;

/// <summary>
/// Téléchargement des leçons : reprise après coupure (requête HTTP Range sur un fichier .part), vérification de la somme de
/// contrôle SHA-256 (EF-22) puis décompression. Une leçon n'est lisible que si elle est complète et vérifiée (RG2).
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly ApiClient _api;
    private readonly LocalStateService _local;
    private readonly LessonStorage _storage;
    private readonly CatalogService _catalog;

    public DownloadService(ApiClient api, LocalStateService local, LessonStorage storage, CatalogService catalog)
    {
        _api = api; _local = local; _storage = storage; _catalog = catalog;
    }

    public async Task<ServiceResult<bool>> DownloadAsync(IEnumerable<string> leconIds, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var ids = leconIds.Distinct().ToList();
        if (ids.Count == 0) return new(true, true);
        var done = 0;
        foreach (var id in ids)
        {
            var r = await DownloadOneAsync(id, ct);
            if (!r.Success) return r;
            progress?.Report((double)(++done) / ids.Count);
        }
        return new(true, true);
    }

    // La reprise est automatique : relancer le téléchargement continue le fichier .part existant.
    public Task PauseAsync(string leconId, CancellationToken ct = default) => Task.CompletedTask;
    public Task ResumeAsync(string leconId, CancellationToken ct = default) => Task.CompletedTask;

    private async Task<ServiceResult<bool>> DownloadOneAsync(string id, CancellationToken ct)
    {
        var dir = _storage.DownloadDir(id);
        Directory.CreateDirectory(dir);
        var part = Path.Combine(dir, "package.zip.part");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            long existing = File.Exists(part) ? new FileInfo(part).Length : 0;
            using var req = _api.CreateRequest(HttpMethod.Get, $"/api/lessons/{Uri.EscapeDataString(id)}/package", auth: true);
            if (existing > 0) req.Headers.Range = new RangeHeaderValue(existing, null);

            string? checksum;
            int? version;
            try
            {
                using var res = await _api.Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                if (res.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable) { File.Delete(part); continue; }
                if (res.StatusCode == HttpStatusCode.Unauthorized)
                    return new(false, false, Tx.T("Session expirée : reconnectez-vous.", "Session expired: please sign in again."));
                if (!res.IsSuccessStatusCode)
                    return new(false, false, res.StatusCode == HttpStatusCode.NotFound
                        ? Tx.T("Cette leçon n'est plus disponible.", "This lesson is no longer available.")
                        : Tx.T($"Erreur serveur ({(int)res.StatusCode}).", $"Server error ({(int)res.StatusCode})."));

                checksum = res.Headers.TryGetValues("X-Checksum-Sha256", out var c) ? c.FirstOrDefault() : null;
                version = res.Headers.TryGetValues("X-Lesson-Version", out var v) && int.TryParse(v.FirstOrDefault(), out var n) ? n : null;
                var append = res.StatusCode == HttpStatusCode.PartialContent;

                await using var src = await res.Content.ReadAsStreamAsync(ct);
                await using var dst = new FileStream(part, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);
                await src.CopyToAsync(dst, 81920, ct);
            }
            catch (Exception ex) when (ex is (HttpRequestException or IOException or TaskCanceledException) && !ct.IsCancellationRequested)
            {
                // Le fichier .part est conservé : le prochain essai reprendra là où il s'est arrêté (EF-17).
                return new(false, false,
                    Tx.T("Connexion interrompue. Relancez : le téléchargement reprendra où il s'est arrêté.",
                         "Connection lost. Retry: the download will resume where it stopped."), Offline: true);
            }

            var actual = await Sha256Async(part, ct);
            if (checksum != null && !string.Equals(actual, checksum, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(part); // fichier corrompu : on repart de zéro
                continue;
            }

            var size = new FileInfo(part).Length;
            var content = _storage.ContentDir(id);
            if (Directory.Exists(content)) Directory.Delete(content, recursive: true);
            ZipFile.ExtractToDirectory(part, content, overwriteFiles: true);
            File.Delete(part);

            var meta = _catalog.FindLecon(id);
            _local.Update(id, l =>
            {
                l.Downloaded = true;
                l.Version = version ?? meta?.Version ?? l.Version;
                l.Checksum = checksum ?? actual;
                l.SizeBytes = size;
                l.DownloadedUtc = DateTime.UtcNow;
                if (meta != null) { l.Titre = meta.Titre; l.MatiereNom = meta.MatiereNom; }
                if (l.Statut is LessonStatus.NotDownloaded or LessonStatus.Failed) l.Statut = LessonStatus.Downloaded;
            });
            return new(true, true);
        }

        return new(false, false, Tx.T("Fichier corrompu : le téléchargement a échoué deux fois.", "Corrupted file: the download failed twice."));
    }

    private static async Task<string> Sha256Async(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(fs, ct)).ToLowerInvariant();
    }
}
