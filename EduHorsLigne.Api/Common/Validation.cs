using System.Text.RegularExpressions;
using EduHorsLigne.Api.Domain;

namespace EduHorsLigne.Api.Common;

public static class Identifiant
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRx = new(@"^(?:\+?237)?(\d{9})$", RegexOptions.Compiled);

    /// <summary>EF-81 : e-mail valide ou numéro camerounais (+237 suivi de 9 chiffres). Normalise pour l'unicité.</summary>
    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim();
        if (EmailRx.IsMatch(s)) { normalized = s.ToLowerInvariant(); return true; }

        var compact = new string(s.Where(c => char.IsDigit(c) || c == '+').ToArray());
        var m = PhoneRx.Match(compact);
        if (m.Success) { normalized = "+237" + m.Groups[1].Value; return true; }
        return false;
    }
}

public static class Rules
{
    public const int MinPasswordLength = 8;

    public static bool PasswordOk(string? p) => !string.IsNullOrEmpty(p) && p.Length >= MinPasswordLength;
    public static bool LangueOk(string? l) => l is "fr" or "en";

    public static string? ValidateBlocks(List<LessonBlockDto>? blocks)
    {
        if (blocks == null) return null;
        if (blocks.Count > 500) return "Trop de blocs (500 maximum).";
        for (var i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            switch (b.Type)
            {
                case BlockType.BulletList:
                    if (b.Items == null || b.Items.Count == 0) return $"Bloc {i + 1} (BulletList) : éléments requis.";
                    break;
                case BlockType.Table:
                    if (b.Rows == null || b.Rows.Count == 0) return $"Bloc {i + 1} (Table) : lignes requises.";
                    break;
                default:
                    if (string.IsNullOrWhiteSpace(b.Text)) return $"Bloc {i + 1} ({b.Type}) : texte requis.";
                    break;
            }
        }
        return null;
    }

    public static string? ValidateQuiz(QuizDto? q)
    {
        if (q == null || q.Questions == null || q.Questions.Count == 0) return "Le quiz doit contenir au moins une question.";
        if (q.Questions.Count > 50) return "Trop de questions (50 maximum).";
        for (var i = 0; i < q.Questions.Count; i++)
        {
            var k = q.Questions[i];
            if (string.IsNullOrWhiteSpace(k.Enonce)) return $"Question {i + 1} : énoncé requis.";
            if (k.Options == null || k.Options.Count < 2) return $"Question {i + 1} : au moins 2 options.";
            if (k.IndexBonneReponse < 0 || k.IndexBonneReponse >= k.Options.Count) return $"Question {i + 1} : index de bonne réponse invalide.";
        }
        return null;
    }
}
