using EduHorsLigne.Helpers;

namespace EduHorsLigne.Views.Controls;

/// <summary>Pastille ronde avec un glyphe (icône) ; Tone : Ok, Warn, Info, Neutral, Primary, Accent.</summary>
public partial class IconCircle : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string), typeof(IconCircle), string.Empty,
        propertyChanged: (b, _, n) => ((IconCircle)b).GlyphLabel.Text = n as string);
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(nameof(Tone), typeof(string), typeof(IconCircle), "Neutral",
        propertyChanged: (b, _, _) => ((IconCircle)b).ApplyTone());
    public static readonly BindableProperty DiameterProperty = BindableProperty.Create(nameof(Diameter), typeof(double), typeof(IconCircle), 40d,
        propertyChanged: (b, _, n) =>
        {
            var c = (IconCircle)b;
            var d = (double)n;
            c.Circle.WidthRequest = d;
            c.Circle.HeightRequest = d;
            c.Circle.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(d / 2) };
            c.GlyphLabel.FontSize = d * 0.45;
        });

    public IconCircle()
    {
        InitializeComponent();
        ApplyTone();
    }

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string Tone { get => (string)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }

    private void ApplyTone()
    {
        Circle.BackgroundColor = Res.GetColor($"Tone{Tone}Bg");
        GlyphLabel.TextColor = Res.GetColor($"Tone{Tone}Fg");
    }
}
