using EduHorsLigne.Helpers;

namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Pastille de statut. Kind accepte un enum (LessonStatus, Difficulty, ExerciseStatus, Verdict) ou une chaîne
/// (« sync_ok », « sync_pending », « sync_failed »). Icône + texte + couleur : jamais la couleur seule.
/// </summary>
public partial class Badge : ContentView
{
    private static readonly Dictionary<string, (string Icon, string Tone)> Map = new()
    {
        ["notdownloaded"] = ("⬇", "Neutral"),
        ["downloaded"] = ("✔", "Info"),
        ["read"] = ("📖", "Info"),
        ["completed"] = ("✅", "Ok"),
        ["updateavailable"] = ("🔄", "Warn"),
        ["failed"] = ("⚠", "Err"),
        ["easy"] = ("●", "Ok"),
        ["medium"] = ("●●", "Warn"),
        ["hard"] = ("●●●", "Err"),
        ["notstarted"] = ("○", "Neutral"),
        ["inprogress"] = ("◐", "Warn"),
        ["done"] = ("✔", "Ok"),
        ["correct"] = ("✓", "Ok"),
        ["partial"] = ("◐", "Warn"),
        ["incorrect"] = ("✗", "Err"),
        ["sync_ok"] = ("✔", "Ok"),
        ["sync_pending"] = ("⏳", "Warn"),
        ["sync_failed"] = ("⚠", "Err"),
    };

    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(object), typeof(Badge), null,
        propertyChanged: (b, _, n) => ((Badge)b).Apply(n?.ToString()?.ToLowerInvariant()));

    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Badge), null,
        propertyChanged: (b, _, n) => ((Badge)b).ApplyFree());
    public static readonly BindableProperty ToneProperty = BindableProperty.Create(nameof(Tone), typeof(string), typeof(Badge), "Neutral",
        propertyChanged: (b, _, n) => ((Badge)b).ApplyFree());
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(Badge), null,
        propertyChanged: (b, _, n) => ((Badge)b).ApplyFree());

    public Badge() => InitializeComponent();

    /// <summary>Texte libre (déjà localisé) ; prioritaire sur Kind.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    /// <summary>Ton : Ok, Warn, Err, Info, Neutral.</summary>
    public string Tone { get => (string)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    private void ApplyFree()
    {
        if (Text is null) return;
        IconLabel.Text = Icon ?? string.Empty;
        IconLabel.IsVisible = !string.IsNullOrEmpty(Icon);
        TextLabel.RemoveBinding(Label.TextProperty);
        TextLabel.Text = Text;
        Pill.BackgroundColor = Res.GetColor($"Tone{Tone}Bg");
        var fg = Res.GetColor($"Tone{Tone}Fg");
        IconLabel.TextColor = fg;
        TextLabel.TextColor = fg;
    }

    public object? Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }

    private void Apply(string? kind)
    {
        if (kind is null || !Map.TryGetValue(kind, out var info)) return;
        IconLabel.IsVisible = true;
        IconLabel.Text = info.Icon;
        TextLabel.SetBinding(Label.TextProperty, new Binding($"[badge_{kind}]", source: Loc.Instance));
        Pill.BackgroundColor = Res.GetColor($"Tone{info.Tone}Bg");
        var fg = Res.GetColor($"Tone{info.Tone}Fg");
        IconLabel.TextColor = fg;
        TextLabel.TextColor = fg;
    }
}
