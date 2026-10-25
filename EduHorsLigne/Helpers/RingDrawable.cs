namespace EduHorsLigne.Helpers;

/// <summary>Anneau de progression (score) dessiné avec GraphicsView, sans bibliothèque de graphiques.</summary>
public class RingDrawable : IDrawable
{
    public RingDrawable(double progress) => Progress = Math.Clamp(progress, 0, 1);
    public double Progress { get; }

    public void Draw(ICanvas canvas, RectF rect)
    {
        const float stroke = 14f;
        var size = Math.Min(rect.Width, rect.Height) - stroke;
        var x = (rect.Width - size) / 2f;
        var y = (rect.Height - size) / 2f;

        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Res.GetColor("SurfaceAltDark") : Res.GetColor("SurfaceAltLight");
        canvas.DrawEllipse(x, y, size, size);

        canvas.StrokeColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Res.GetColor("PrimaryOnDarkColor") : Res.GetColor("PrimaryDarkColor");
        if (Progress > 0.001)
            canvas.DrawArc(x, y, size, size, 90, (float)(90 - 360 * Progress), true, false);
    }
}
