namespace EduHorsLigne.Helpers;

/// <summary>Classes de taille d'écran : Compact (&lt;600 dp), Medium (600-840), Expanded (&gt;840).</summary>
public enum SizeClass { Compact, Medium, Expanded }

/// <summary>Informations d'affichage responsive (basées sur DeviceDisplay).</summary>
public static class ScreenInfo
{
    static ScreenInfo()
    {
        DeviceDisplay.Current.MainDisplayInfoChanged += (_, _) => Changed?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Levé lors d'un changement de taille ou d'orientation.</summary>
    public static event EventHandler? Changed;

    public static SizeClass Classify(double widthDp) =>
        widthDp < 600 ? SizeClass.Compact : widthDp <= 840 ? SizeClass.Medium : SizeClass.Expanded;

    public static SizeClass Current
    {
        get
        {
            var info = DeviceDisplay.Current.MainDisplayInfo;
            return Classify(info.Width / info.Density);
        }
    }

    public static bool IsLandscape => DeviceDisplay.Current.MainDisplayInfo.Orientation == DisplayOrientation.Landscape;
}
