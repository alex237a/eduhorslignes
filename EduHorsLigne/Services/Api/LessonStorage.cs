using System.Text.Json;

namespace EduHorsLigne.Services.Api;

/// <summary>Fichiers des leçons téléchargées : AppData/lessons/{id}/content/(lesson.json, quiz.json, images...).</summary>
public class LessonStorage
{
    private static string Root => Path.Combine(FileSystem.AppDataDirectory, "lessons");

    public string DownloadDir(string id) => Path.Combine(Root, id);
    public string ContentDir(string id) => Path.Combine(Root, id, "content");

    public bool HasContent(string id) => File.Exists(Path.Combine(ContentDir(id), "lesson.json"));

    public async Task<LessonFile?> ReadLessonAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(ContentDir(id), "lesson.json");
        if (!File.Exists(path)) return null;
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<LessonFile>(fs, JsonDefaults.Options, ct);
    }

    public async Task<QuizFile?> ReadQuizAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(ContentDir(id), "quiz.json");
        if (!File.Exists(path)) return null;
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<QuizFile>(fs, JsonDefaults.Options, ct);
    }

    public void Delete(string id)
    {
        var dir = DownloadDir(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    public void DeleteAll()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
}
