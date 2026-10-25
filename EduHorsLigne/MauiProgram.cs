using EduHorsLigne.Helpers;
using EduHorsLigne.Services;
using EduHorsLigne.Services.Api;
using EduHorsLigne.Services.Interfaces;
using EduHorsLigne.Services.Mock;
using EduHorsLigne.ViewModels;
using EduHorsLigne.Views;
using Microsoft.Extensions.Logging;

namespace EduHorsLigne;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        CrashLog.Install();
        CrashLog.Info("MauiProgram.CreateMauiApp : début");
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<MockDataProvider>(); // encore utilisé par la recherche, la bibliothèque, les exercices, le stockage

        // Backend (API REST EduHorsLigne.Api) : adresse dans Services/Api/ApiSettings.cs
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<LocalStateService>();
        builder.Services.AddSingleton<LessonStorage>();
        builder.Services.AddSingleton<CatalogService>();
        builder.Services.AddSingleton<ICatalogueService>(sp => sp.GetRequiredService<CatalogService>());
        builder.Services.AddSingleton<IAuthService, ApiAuthService>();
        builder.Services.AddSingleton<IDownloadService, DownloadService>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<ISyncService>(sp => sp.GetRequiredService<SyncService>());

        // Pages + ViewModels
        AddPage<WelcomePage, WelcomeViewModel>(builder);
        AddPage<LoginPage, LoginViewModel>(builder);
        AddPage<RegisterPage, RegisterViewModel>(builder);
        AddPage<ForgotPasswordPage, ForgotPasswordViewModel>(builder);
        AddPage<OtpPage, OtpViewModel>(builder);
        AddPage<HomePage, HomeViewModel>(builder);
        AddPage<SubjectPage, SubjectViewModel>(builder);
        AddPage<LessonDetailPage, LessonDetailViewModel>(builder);
        AddPage<ReaderPage, ReaderViewModel>(builder);
        AddPage<SearchPage, SearchViewModel>(builder);
        AddPage<LibraryPage, LibraryViewModel>(builder);
        AddPage<ProgressPage, ProgressViewModel>(builder);
        AddPage<StoragePage, StorageViewModel>(builder);
        AddPage<QuizPage, QuizViewModel>(builder);
        AddPage<ExercisesHomePage, ExercisesHomeViewModel>(builder);
        AddPage<ExerciseCorrectionPage, ExerciseCorrectionViewModel>(builder);
        AddPage<ExerciseSessionPage, ExerciseSessionViewModel>(builder);
        AddPage<ExerciseResultPage, ExerciseResultViewModel>(builder);
        AddPage<ProfilePage, ProfileViewModel>(builder);
        AddPage<SettingsPage, SettingsViewModel>(builder);
        AddPage<HelpPage, HelpViewModel>(builder);
        AddPage<GalleryPage, GalleryViewModel>(builder);

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        CrashLog.Info("MauiProgram.CreateMauiApp : services construits");
        return app;
    }

    private static void AddPage<TPage, TViewModel>(MauiAppBuilder builder)
        where TPage : Page
        where TViewModel : class
    {
        builder.Services.AddTransient<TViewModel>();
        builder.Services.AddTransient<TPage>();
    }
}
