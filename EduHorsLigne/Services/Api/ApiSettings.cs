namespace EduHorsLigne.Services.Api;

/// <summary>
/// Adresse de l'API. Par défaut : localhost (Windows) ou 10.0.2.2 (émulateur Android, qui désigne le PC hôte).
/// Pour un téléphone réel, mettre l'adresse IP du PC, par ex. « http://192.168.1.20:5080 » (voir README-LANCEMENT.md).
/// </summary>
public static class ApiSettings
{
    private const string Key = "api_base_url";

    public static string DefaultBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    public static string BaseUrl
    {
        get => Preferences.Default.Get(Key, DefaultBaseUrl);
        set => Preferences.Default.Set(Key, (value ?? "").Trim().TrimEnd('/'));
    }
}
