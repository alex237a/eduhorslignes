using System.Diagnostics;

namespace EduHorsLigne.Helpers;

/// <summary>
/// Journal de démarrage et de plantage : écrit « startup.log » à côté de l'exécutable (ou dans %TEMP%).
/// Indispensable sous Windows, où une erreur au démarrage ferme la fenêtre sans aucun message.
/// </summary>
public static class CrashLog
{
    private static readonly object Gate = new();
    private static bool _installed;

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "startup.log");

    public static void Info(string message) => Append($"[{DateTime.Now:HH:mm:ss.fff}] {message}");

    public static void Error(string source, Exception? ex) => Append($"[{DateTime.Now:HH:mm:ss.fff}] ERREUR ({source})\n{ex}\n");

    /// <summary>Branche les gestionnaires d'exceptions non gérées (une seule fois).</summary>
    public static void Install()
    {
        if (_installed) return;
        _installed = true;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Error("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Error("Task", e.Exception);
    }

    private static void Append(string line)
    {
        Debug.WriteLine(line);
        lock (Gate)
        {
            try { File.AppendAllText(FilePath, line + Environment.NewLine); }
            catch
            {
                try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "eduhorsligne-startup.log"), line + Environment.NewLine); }
                catch { /* rien de plus à faire */ }
            }
        }
    }
}
