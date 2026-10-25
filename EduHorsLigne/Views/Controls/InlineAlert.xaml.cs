using EduHorsLigne.Helpers;

namespace EduHorsLigne.Views.Controls;

/// <summary>Message d'erreur / succès / information avec icône (Kind : error, success, info, warn).</summary>
public partial class InlineAlert : ContentView
{
    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(string), typeof(InlineAlert), "info",
        propertyChanged: (b, _, n) => ((InlineAlert)b).Apply(n as string));
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(InlineAlert), string.Empty,
        propertyChanged: (b, _, n) => ((InlineAlert)b).TextLabel.Text = n as string);

    public InlineAlert()
    {
        InitializeComponent();
        Apply("info");
    }

    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private void Apply(string? kind)
    {
        var (icon, tone) = kind switch
        {
            "error" => ("⚠", "Err"),
            "success" => ("✅", "Ok"),
            "warn" => ("ℹ", "Warn"),
            _ => ("ℹ", "Info")
        };
        IconLabel.Text = icon;
        Box.BackgroundColor = Res.GetColor($"Tone{tone}Bg");
        IconLabel.TextColor = Res.GetColor($"Tone{tone}Fg");
        TextLabel.TextColor = Res.GetColor($"Tone{tone}Fg");
    }
}
