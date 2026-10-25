namespace EduHorsLigne.Helpers;

/// <summary>
/// Ajuste le nombre de colonnes d'une CollectionView selon sa largeur réelle :
/// 1 colonne (Compact), 2 (Medium), 3 (Expanded) par défaut.
/// </summary>
public class ResponsiveSpanBehavior : Behavior<CollectionView>
{
    public int Compact { get; set; } = 1;
    public int Medium { get; set; } = 2;
    public int Expanded { get; set; } = 3;

    private CollectionView? _view;
    private int _current;

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
        _view = bindable;
        bindable.SizeChanged += OnSizeChanged;
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        bindable.SizeChanged -= OnSizeChanged;
        _view = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (_view is null || _view.Width <= 0) return;
        var span = ScreenInfo.Classify(_view.Width) switch
        {
            SizeClass.Compact => Compact,
            SizeClass.Medium => Medium,
            _ => Expanded
        };
        if (span == _current) return;
        _current = span;
        _view.ItemsLayout = new GridItemsLayout(span, ItemsLayoutOrientation.Vertical)
        {
            VerticalItemSpacing = 8,
            HorizontalItemSpacing = 8
        };
    }
}
