using EduHorsLigne.ViewModels;

namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Recouvre la page quand son état n'est pas « Contenu » : chargement (skeleton), vide ou erreur.
/// À placer en dernier enfant du Grid racine d'une page de données.
/// </summary>
public partial class StateOverlay : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(nameof(State), typeof(PageState), typeof(StateOverlay), PageState.Content,
        propertyChanged: (b, _, n) => ((StateOverlay)b).Apply((PageState)n));

    public static readonly BindableProperty ShowEmptyProperty = BindableProperty.Create(nameof(ShowEmpty), typeof(bool), typeof(StateOverlay), true);

    /// <summary>Mettre à false si la page dessine elle-même son état vide (ex. Accueil).</summary>
    public bool ShowEmpty { get => (bool)GetValue(ShowEmptyProperty); set => SetValue(ShowEmptyProperty, value); }

    public StateOverlay()
    {
        InitializeComponent();
        // Démo : « Réessayer » revient à l'état Contenu.
        ErrorView.Command = new Command(() => DemoState.Set(PageState.Content));
    }

    public PageState State { get => (PageState)GetValue(StateProperty); set => SetValue(StateProperty, value); }

    private void Apply(PageState state)
    {
        Root.IsVisible = state == PageState.Loading || state == PageState.Error || (state == PageState.Empty && ShowEmpty);
        LoadingView.IsVisible = state == PageState.Loading;
        Spinner.IsRunning = state == PageState.Loading;
        EmptyView.IsVisible = state == PageState.Empty;
        ErrorView.IsVisible = state == PageState.Error;
    }
}
