using EduHorsLigne.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace EduHorsLigne.Views.Controls;

/// <summary>
/// Barre de choix : pastilles défilantes (Mode = Chips), onglets segmentés (Tabs) ou segmenté plein (Segmented).
/// Items = libellés ; SelectedIndex est lié en lecture/écriture.
/// </summary>
public class ChipBar : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable<string>), typeof(ChipBar), null,
        propertyChanged: (b, _, _) => ((ChipBar)b).Rebuild());
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ChipBar), 0,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((ChipBar)b).Rebuild());
    public static readonly BindableProperty ModeProperty = BindableProperty.Create(nameof(Mode), typeof(string), typeof(ChipBar), "Chips",
        propertyChanged: (b, _, _) => ((ChipBar)b).Rebuild());

    public ChipBar()
    {
        if (Application.Current is { } app) app.RequestedThemeChanged += (_, _) => Rebuild();
    }

    public IEnumerable<string>? Items { get => (IEnumerable<string>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public string Mode { get => (string)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    private void Rebuild()
    {
        var items = Items?.ToList();
        if (items is null || items.Count == 0) { Content = null; return; }

        var chips = Mode == "Chips";
        var host = chips
            ? (Layout)new HorizontalStackLayout { Spacing = 8 }
            : new Grid { ColumnSpacing = 4 };

        for (var i = 0; i < items.Count; i++)
        {
            var index = i;
            var selected = i == SelectedIndex;
            var (bg, fg, stroke) = Colors(selected);
            var label = new Label
            {
                Text = (Mode == "Segmented" && selected ? "✓ " : string.Empty) + items[i] + (chips && selected && i == 0 ? " ●" : string.Empty),
                FontSize = 14,
                FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None,
                TextColor = fg,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            var border = new Border
            {
                BackgroundColor = bg,
                Stroke = new SolidColorBrush(stroke),
                StrokeThickness = chips && !selected ? 1 : 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(chips ? 20 : 10) },
                Padding = new Thickness(14, 0),
                MinimumHeightRequest = 44,
                Content = label
            };
            SemanticProperties.SetDescription(border, items[i]);
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectedIndex = index;
            border.GestureRecognizers.Add(tap);
            if (host is Grid grid)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetColumn(border, i);
            }
            host.Add(border);
        }

        if (chips)
        {
            Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = host };
        }
        else
        {
            Content = new Border
            {
                BackgroundColor = Res.Themed("SurfaceAltLight", "SurfaceAltDark"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
                Padding = 4,
                Content = host
            };
        }
    }

    private (Color Bg, Color Fg, Color Stroke) Colors(bool selected)
    {
        var text = Res.Themed("TextLight", "TextDark");
        var surface = Res.Themed("SurfaceLight", "SurfaceDark");
        var outline = Res.Themed("OutlineLight", "OutlineDark");
        return Mode switch
        {
            "Chips" => selected
                ? (Res.Themed("PrimaryDarkColor", "PrimaryOnDarkColor"), Res.Themed("White", "OnPrimaryDarkColor"), Colors2.Transparent)
                : (surface, text, outline),
            "Tabs" => selected
                ? (surface, Res.Themed("PrimaryDarkColor", "PrimaryOnDarkColor"), Colors2.Transparent)
                : (Colors2.Transparent, Res.Themed("MutedLight", "MutedDark"), Colors2.Transparent),
            _ => selected
                ? (Res.Themed("PrimaryDarkColor", "PrimaryOnDarkColor"), Res.Themed("White", "OnPrimaryDarkColor"), Colors2.Transparent)
                : (Colors2.Transparent, text, Colors2.Transparent)
        };
    }

    private static class Colors2
    {
        public static readonly Color Transparent = Microsoft.Maui.Graphics.Colors.Transparent;
    }
}
