using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EduHorsLigne.Services.Api;

public record ApiResult<T>(bool Ok, T? Data, ApiError? Error, bool Offline, int Status);

/// <summary>Client HTTP de l'API. Ne lève jamais d'exception réseau : renvoie un ApiResult (Offline = serveur injoignable).</summary>
public class ApiClient
{
    private readonly SessionService _session;

    public ApiClient(SessionService session) => _session = session;

    public HttpClient Http { get; } = new() { Timeout = TimeSpan.FromSeconds(30) };

    public string Url(string path) => ApiSettings.BaseUrl.TrimEnd('/') + path;

    public HttpRequestMessage CreateRequest(HttpMethod method, string path, bool auth)
    {
        var req = new HttpRequestMessage(method, Url(path));
        if (auth && !string.IsNullOrEmpty(_session.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
        return req;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, bool auth = false, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, auth, ct);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body, bool auth = false, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, auth, ct);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, bool auth = true, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, auth, ct);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, bool auth, CancellationToken ct)
    {
        try
        {
            using var req = CreateRequest(method, path, auth);
            if (body is not null)
                req.Content = new StringContent(JsonSerializer.Serialize(body, JsonDefaults.Options), Encoding.UTF8, "application/json");

            using var res = await Http.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            var status = (int)res.StatusCode;

            if (res.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text) && typeof(T) != typeof(object))
                    data = JsonSerializer.Deserialize<T>(text, JsonDefaults.Options);
                return new ApiResult<T>(true, data, null, false, status);
            }

            ApiError? err = null;
            try { err = JsonSerializer.Deserialize<ApiError>(text, JsonDefaults.Options); } catch { /* corps non JSON */ }
            err ??= new ApiError($"http_{status}", $"Erreur serveur ({status}).");
            return new ApiResult<T>(false, default, err, false, status);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            var offline = ex is not JsonException;
            var msg = offline
                ? Helpers.Tx.T($"Serveur injoignable ({ApiSettings.BaseUrl}). Vérifiez votre connexion.",
                               $"Server unreachable ({ApiSettings.BaseUrl}). Check your connection.")
                : Helpers.Tx.T("Réponse du serveur illisible.", "Unreadable server response.");
            return new ApiResult<T>(false, default, new ApiError(offline ? "hors_ligne" : "reponse_invalide", msg), offline, 0);
        }
    }
}
