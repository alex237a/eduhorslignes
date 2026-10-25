using System.ComponentModel;
using EduHorsLigne.Helpers;
using EduHorsLigne.Models;
using EduHorsLigne.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace EduHorsLigne.Views;

/// <summary>Lecteur : construit l'affichage à partir des blocs du fichier lesson.json téléchargé (lecture 100 % hors ligne).</summary>
public partial class ReaderPage : ContentPage
{
    private readonly ReaderViewModel _vm;
    private string? _restoredFor;

    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        _vm.PropertyChanged += OnViewModelChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        Render();
        await RestorePositionAsync();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReaderViewModel.Blocks) or nameof(ReaderViewModel.IsDark)
            or nameof(ReaderViewModel.ReaderFontSize) or nameof(ReaderViewModel.Title))
            Render();
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        var max = Scroller.ContentSize.Height - Scroller.Height;
        if (max <= 1) return;
        _vm.OnScrolled(Math.Clamp(e.ScrollY / max, 0, 1));
    }

    /// <summary>La leçon rouverte reprend là où l'élève s'était arrêté (EF-28).</summary>
    private async Task RestorePositionAsync()
    {
        if (_vm.LessonId is null || _restoredFor == _vm.LessonId) return;
        _restoredFor = _vm.LessonId;
        if (_vm.ResumeAt < 0.02) return;
        await Task.Delay(250);
        var max = Scroller.ContentSize.Height - Scroller.Height;
        if (max > 1) await Scroller.ScrollToAsync(0, max * _vm.ResumeAt, false);
    }

    // ---------- Construction du contenu ----------

    private void Render()
    {
        LessonHost.Children.Clear();
        if (_vm.Blocks.Count == 0 && string.IsNullOrEmpty(_vm.Title)) return;

        var dark = _vm.IsDark;
        var size = _vm.ReaderFontSize;
        var text = Res.GetColor(dark ? "TextDark" : "TextLight");
        var muted = Res.GetColor(dark ? "MutedDark" : "MutedLight");
        var accent = Res.GetColor(dark ? "PrimaryOnDarkColor" : "PrimaryDarkColor");

        LessonHost.Children.Add(new Label { Text = _vm.Title, FontSize = _vm.TitleSize, FontAttributes = FontAttributes.Bold, TextColor = text });
        LessonHost.Children.Add(new Label { Text = _vm.MetaText, FontSize = 13, TextColor = muted });

        foreach (var b in _vm.Blocks)
        {
            View? v = b.Type switch
            {
                BlockType.Heading => new Label
                {
                    Text = b.Text, FontSize = size + 4, FontAttributes = FontAttributes.Bold, TextColor = accent,
                    Margin = new Thickness(0, 8, 0, 0)
                },
                BlockType.Paragraph => Body(b.Text, size, text),
                BlockType.Definition => Box(Body(b.Text, size, dark ? text : Res.GetColor("ToneOkFg")),
                                            dark ? Res.GetColor("SurfaceDark") : Res.GetColor("ToneOkBg")),
                BlockType.Example => Box(Body(b.Text, size, dark ? text : Res.GetColor("ToneWarnFg")),
                                         dark ? Res.GetColor("SurfaceAltDark") : Res.GetColor("ToneWarnBg")),
                BlockType.Formula => Box(new Label
                {
                    Text = b.Text, FontSize = size + 2, FontAttributes = FontAttributes.Bold, TextColor = accent,
                    HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center
                }, dark ? Res.GetColor("SurfaceDark") : Res.GetColor("SurfaceAltLight")),
                BlockType.BulletList => Bullets(b.Items, size, text),
                BlockType.Table => Table(b.Rows, size, text, accent, dark),
                _ => null
            };
            if (v != null) LessonHost.Children.Add(v);
        }
    }

    private static Label Body(string? text, double size, Color color) =>
        new() { Text = text, FontSize = size, LineHeight = 1.4, TextColor = color };

    private static Border Box(View content, Color background) => new()
    {
        Content = content,
        BackgroundColor = background,
        StrokeThickness = 0,
        Padding = new Thickness(14),
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) }
    };

    private static View Bullets(List<string> items, double size, Color text)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        foreach (var item in items) stack.Children.Add(Body("•  " + item, size, text));
        return stack;
    }

    private static View Table(List<TableRow> rows, double size, Color text, Color accent, bool dark)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 8,
            RowSpacing = 6,
            Padding = new Thickness(10)
        };
        for (var r = 0; r < rows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var cells = new[] { rows[r].C1, rows[r].C2, rows[r].C3 };
            for (var c = 0; c < 3; c++)
                grid.Add(new Label
                {
                    Text = cells[c], FontSize = size - 1,
                    FontAttributes = r == 0 ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = r == 0 ? accent : text
                }, c, r);
        }
        return new Border
        {
            Content = grid,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Res.GetColor(dark ? "OutlineDark" : "OutlineLight")),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) }
        };
    }
}
