namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Barre d'application de la maquette : retour, logo « EH », titre, contenu droit facultatif,
/// pastille « Hors-ligne » et bouton profil.
/// </summary>
public partial class AppBar : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(AppBar), string.Empty,
        propertyChanged: (b, _, n) => ((AppBar)b).TitleLabel.Text = n as string);
    public static readonly BindableProperty ShowBackProperty = BindableProperty.Create(nameof(ShowBack), typeof(bool), typeof(AppBar), false,
        propertyChanged: (b, _, n) => ((AppBar)b).BackButton.IsVisible = (bool)n);
    public static readonly BindableProperty ShowLogoProperty = BindableProperty.Create(nameof(ShowLogo), typeof(bool), typeof(AppBar), false,
        propertyChanged: (b, _, n) => ((AppBar)b).LogoBadge.IsVisible = (bool)n);
    public static readonly BindableProperty ShowOfflineProperty = BindableProperty.Create(nameof(ShowOffline), typeof(bool), typeof(AppBar), false,
        propertyChanged: (b, _, n) => ((AppBar)b).OfflineChip.IsVisible = (bool)n);
    public static readonly BindableProperty ShowAvatarProperty = BindableProperty.Create(nameof(ShowAvatar), typeof(bool), typeof(AppBar), true,
        propertyChanged: (b, _, n) => ((AppBar)b).AvatarButton.IsVisible = (bool)n);
    public static readonly BindableProperty RightContentProperty = BindableProperty.Create(nameof(RightContent), typeof(View), typeof(AppBar), null,
        propertyChanged: (b, _, n) => ((AppBar)b).RightHost.Content = n as View);

    public AppBar() => InitializeComponent();

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public bool ShowLogo { get => (bool)GetValue(ShowLogoProperty); set => SetValue(ShowLogoProperty, value); }
    public bool ShowOffline { get => (bool)GetValue(ShowOfflineProperty); set => SetValue(ShowOfflineProperty, value); }
    public bool ShowAvatar { get => (bool)GetValue(ShowAvatarProperty); set => SetValue(ShowAvatarProperty, value); }
    public View? RightContent { get => (View?)GetValue(RightContentProperty); set => SetValue(RightContentProperty, value); }

    private async void OnBack(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnAvatar(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//main/profile");
}
