namespace EduHorsLigne.Helpers;

/// <summary>Petit utilitaire pour les messages produits par le code (non présents dans les .resx).</summary>
public static class Tx
{
    public static string T(string fr, string en) => Loc.Instance.Language == "fr" ? fr : en;

    public static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} o",
        < 1024 * 1024 => $"{bytes / 1024.0:0} Ko",
        < 1024L * 1024 * 1024 => $"{bytes / 1048576.0:0.0} Mo",
        _ => $"{bytes / 1073741824.0:0.0} Go"
    };
}
