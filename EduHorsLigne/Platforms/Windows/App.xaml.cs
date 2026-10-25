using EduHorsLigne.Helpers;

namespace EduHorsLigne.WinUI;

/// <summary>Point d'entrée Windows (WinUI 3) de l'application MAUI.</summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        CrashLog.Install();
        CrashLog.Info("WinUI App : constructeur");
        // Journalise l'exception, sans la marquer comme gérée : le comportement normal est conservé.
        UnhandledException += (_, e) => CrashLog.Error("WinUI : " + e.Message, e.Exception);
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
        try { return MauiProgram.CreateMauiApp(); }
        catch (Exception ex)
        {
            CrashLog.Error("CreateMauiApp", ex);
            throw;
        }
    }
}
