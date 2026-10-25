namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Conteneur qui centre son contenu et limite sa largeur (formulaires sur grand écran).
/// Sur téléphone (largeur &lt; MaxContentWidth) le contenu occupe toute la largeur.
/// </summary>
[ContentProperty(nameof(Content))]
public class CenteredContent : ContentView
{
    public static readonly BindableProperty MaxContentWidthProperty =
        BindableProperty.Create(nameof(MaxContentWidth), typeof(double), typeof(CenteredContent), 480d);

    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    public CenteredContent()
    {
        HorizontalOptions = LayoutOptions.Fill;
        SizeChanged += (_, _) =>
        {
            if (Content is not View child || Width <= 0) return;
            child.HorizontalOptions = LayoutOptions.Center;
            var target = Math.Min(Width, MaxContentWidth);
            if (Math.Abs(child.WidthRequest - target) > 0.5) child.WidthRequest = target;
        };
    }
}
