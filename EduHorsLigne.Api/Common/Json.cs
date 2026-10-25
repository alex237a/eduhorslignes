using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduHorsLigne.Api.Common;

public static class Json
{
    /// <summary>Options partagées : camelCase, énumérations en texte, null omis.</summary>
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Garde les accents tels quels (paquets plus légers que \u00E9) ; sans risque pour du JSON d'API.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static List<LessonBlockDto> ParseBlocks(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<LessonBlockDto>>(json, Options) ?? new();

    public static List<QuestionDto> ParseQuestions(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<QuestionDto>>(json, Options) ?? new();

    public static string ToJson(object value) => JsonSerializer.Serialize(value, Options);
}
