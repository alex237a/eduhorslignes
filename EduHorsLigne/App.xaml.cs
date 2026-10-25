using EduHorsLigne.Helpers;

namespace EduHorsLigne;

public partial class App : Application
{
    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState? activationState)
    {
        CrashLog.Info("App.CreateWindow : création du Shell");
        var window = new Window(new AppShell()) { Title = "ÉduHors-Ligne" };
        CrashLog.Info("App.CreateWindow : Shell créé");
#if WINDOWS
        // Sous Windows, la fenêtre s'ouvre à une taille de téléphone ; elle reste redimensionnable
        // (tirer sur les bords pour tester les modes Compact, Medium et Expanded).
        window.Width = 430;
        window.Height = 820;
        window.X = 80;
        window.Y = 40;
        window.MinimumWidth = 320;
        window.MinimumHeight = 480;
#endif
        return window;
    }
}
