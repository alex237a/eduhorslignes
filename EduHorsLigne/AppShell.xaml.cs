using EduHorsLigne.Helpers;
using EduHorsLigne.Services.Api;
using EduHorsLigne.Views;

namespace EduHorsLigne;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        CrashLog.Info("AppShell : XAML chargé");

        // Pages secondaires (poussées au-dessus des onglets ou du flux d'authentification).
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("forgot", typeof(ForgotPasswordPage));
        Routing.RegisterRoute("otp", typeof(OtpPage));
        Routing.RegisterRoute("subject", typeof(SubjectPage));
        Routing.RegisterRoute("lesson", typeof(LessonDetailPage));
        Routing.RegisterRoute("reader", typeof(ReaderPage));
        Routing.RegisterRoute("quiz", typeof(QuizPage));
        Routing.RegisterRoute("exercises", typeof(ExercisesHomePage));
        Routing.RegisterRoute("correction", typeof(ExerciseCorrectionPage));
        Routing.RegisterRoute("session", typeof(ExerciseSessionPage));
        Routing.RegisterRoute("result", typeof(ExerciseResultPage));
        Routing.RegisterRoute("storage", typeof(StoragePage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("help", typeof(HelpPage));
        Routing.RegisterRoute("gallery", typeof(GalleryPage));

        // Session conservée sur l'appareil (EF-03, EF-84) : pas de nouvelle connexion, même hors ligne.
        Loaded += async (_, _) =>
        {
            var session = IPlatformApplication.Current?.Services.GetService<SessionService>();
            if (session is { IsLoggedIn: true }) await GoToAsync("//main/home");
        };
    }
}
