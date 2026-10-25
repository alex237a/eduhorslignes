using EduHorsLigne.Api.Common;
using EduHorsLigne.Api.Domain;
using EduHorsLigne.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EduHorsLigne.Api.Data;

/// <summary>
/// Données de démonstration crédibles : 2 sous-systèmes, plusieurs classes, matières, chapitres, leçons publiées
/// (avec paquets ZIP déjà générés) et quiz. Exécuté une seule fois au premier démarrage.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PackageService packages, IConfiguration cfg, ILogger logger)
    {
        if (!cfg.GetValue("Seed:Enabled", true)) return;

        // ----- Comptes -----
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var adminId = cfg["Seed:AdminIdentifiant"] ?? "admin@eduhorsligne.cm";
            db.Users.Add(new User
            {
                Prenom = "Admin", Nom = "Contenu", Identifiant = adminId.ToLowerInvariant(), Role = UserRole.Admin,
                PasswordHash = PasswordHelper.Hash(cfg["Seed:AdminPassword"] ?? "Admin#2026!")
            });
            logger.LogWarning("Compte administrateur créé : {Id}. Changez le mot de passe par défaut avant tout déploiement.", adminId);
        }

        if (await db.Classes.AnyAsync())
        {
            await db.SaveChangesAsync();
            return;
        }

        // ----- Classes -----
        var classes = new[]
        {
            new Classe { Id = "3e",      Nom = "3e",                   Cycle = "1er cycle", SousSysteme = SousSysteme.Francophone, Ordre = 1 },
            new Classe { Id = "tle-c",   Nom = "Terminale C",          Cycle = "2nd cycle", SousSysteme = SousSysteme.Francophone, Ordre = 2 },
            new Classe { Id = "form5",   Nom = "Form 5",               Cycle = "1st cycle", SousSysteme = SousSysteme.Anglophone,  Ordre = 1 },
            new Classe { Id = "upper6",  Nom = "Upper Sixth Science",  Cycle = "2nd cycle", SousSysteme = SousSysteme.Anglophone,  Ordre = 2 },
        };
        db.Classes.AddRange(classes);

        // ----- Matières -----
        Matiere M(string id, string classe, string nom, string ini, string icone, string ton) =>
            new() { Id = id, ClasseId = classe, Nom = nom, Initiale = ini, Icone = icone, Ton = ton };
        db.Matieres.AddRange(
            M("tle-c-math", "tle-c", "Mathématiques", "M", "Σ", "Ok"),
            M("tle-c-pc", "tle-c", "Physique-Chimie", "P", "⚛", "Warn"),
            M("3e-math", "3e", "Mathématiques", "M", "Σ", "Ok"),
            M("3e-fr", "3e", "Français", "F", "Aa", "Neutral"),
            M("form5-math", "form5", "Mathematics", "M", "Σ", "Ok"),
            M("form5-eng", "form5", "English Language", "E", "Aa", "Neutral"),
            M("upper6-phy", "upper6", "Physics", "P", "⚛", "Warn"));

        // ----- Chapitres -----
        Chapitre C(string id, string matiere, int ordre, string titre) => new() { Id = id, MatiereId = matiere, Ordre = ordre, Titre = titre };
        db.Chapitres.AddRange(
            C("c-tle-lim", "tle-c-math", 1, "Chapitre 1 · Limites et continuité"),
            C("c-tle-suites", "tle-c-math", 2, "Chapitre 2 · Suites numériques"),
            C("c-tle-der", "tle-c-math", 3, "Chapitre 3 · Dérivation"),
            C("c-tle-meca", "tle-c-pc", 1, "Chapitre 1 · Mécanique"),
            C("c-3e-geo", "3e-math", 1, "Chapitre 1 · Géométrie plane"),
            C("c-3e-gram", "3e-fr", 1, "Chapitre 1 · Grammaire"),
            C("c-f5-alg", "form5-math", 1, "Chapter 1 · Algebra"),
            C("c-f5-gram", "form5-eng", 1, "Chapter 1 · Grammar"),
            C("c-u6-mech", "upper6-phy", 1, "Chapter 1 · Mechanics"));

        // ----- Leçons -----
        var lecons = new List<Lecon>
        {
            L("l-limite", "c-tle-lim", 1, "Notion de limite", 12,
                H("Notion de limite"),
                P("Dire que f(x) tend vers ℓ quand x tend vers a signifie que f(x) peut être rendu aussi proche de ℓ que l'on veut, pourvu que x soit suffisamment proche de a."),
                Def("Définition : on note lim f(x) = ℓ lorsque x → a si, pour tout ε > 0, il existe δ > 0 tel que |x − a| < δ entraîne |f(x) − ℓ| < ε."),
                Ex("Exemple : lim (2x + 1) = 7 quand x → 3."),
                H("Limites usuelles"),
                B("lim 1/x = 0 quand x → +∞", "lim x² = +∞ quand x → +∞", "lim 1/x = +∞ quand x → 0⁺")),
            L("l-limite-op", "c-tle-lim", 2, "Limites et opérations", 9,
                H("Opérations sur les limites"),
                P("La limite d'une somme, d'un produit ou d'un quotient se calcule à partir des limites de chaque terme, sauf dans les cas indéterminés."),
                Tbl(("Forme", "Résultat", "Remarque"), ("+∞ + ∞", "+∞", "déterminée"), ("+∞ − ∞", "?", "indéterminée"), ("0 × ∞", "?", "indéterminée"))),
            L("l-continuite", "c-tle-lim", 3, "Continuité en un point", 15,
                H("Continuité en un point"),
                Def("Définition : f est continue en a si lim f(x) = f(a) quand x → a."),
                P("Une fonction polynôme est continue en tout réel. Une fonction rationnelle est continue sur son ensemble de définition.")),
            L("l-tvi", "c-tle-lim", 4, "Théorème des valeurs intermédiaires", 11,
                H("Théorème des valeurs intermédiaires"),
                Def("Théorème : si f est continue sur [a ; b], alors pour tout réel k compris entre f(a) et f(b), l'équation f(x) = k admet au moins une solution c dans [a ; b]."),
                Ex("Exemple : f(x) = x³ + x − 1 vérifie f(0) = −1 et f(1) = 1 ; l'équation f(x) = 0 a donc une solution dans [0 ; 1].")),

            L("l-suites-arith", "c-tle-suites", 1, "Suites arithmétiques", 10,
                H("Suites arithmétiques"),
                P("Une suite arithmétique est une suite de nombres où l'on passe d'un terme au suivant en ajoutant toujours le même nombre."),
                Def("Définition : une suite (uₙ) est arithmétique s'il existe un réel r, appelé raison, tel que pour tout entier n : uₙ₊₁ = uₙ + r."),
                F("uₙ = u₀ + n × r"),
                Ex("Exemple : la suite de premier terme u₀ = 3 et de raison r = 4 donne 3 ; 7 ; 11 ; 15 ; ..."),
                H("Propriétés"),
                B("Si r > 0, la suite est strictement croissante.", "Si r < 0, la suite est strictement décroissante.", "Si r = 0, la suite est constante."),
                Tbl(("Raison r", "Sens de variation", "Exemple"), ("r > 0", "Croissante", "2 ; 5 ; 8"), ("r < 0", "Décroissante", "9 ; 6 ; 3")),
                H("Somme des premiers termes"),
                F("S = (n + 1) × (u₀ + uₙ) / 2"),
                P("Cette formule permet de calculer rapidement la somme de termes consécutifs sans les additionner un par un.")),
            L("l-suites-geo", "c-tle-suites", 2, "Suites géométriques", 10,
                H("Suites géométriques"),
                Def("Définition : une suite (uₙ) est géométrique s'il existe un réel q, appelé raison, tel que uₙ₊₁ = q × uₙ."),
                F("uₙ = u₀ × qⁿ"),
                Ex("Exemple : u₀ = 2 et q = 3 donnent 2 ; 6 ; 18 ; 54 ; ...")),

            L("l-derivee", "c-tle-der", 1, "Nombre dérivé et tangente", 14,
                H("Nombre dérivé"),
                Def("Définition : f est dérivable en a si le taux d'accroissement (f(a + h) − f(a)) / h a une limite finie quand h → 0. Cette limite est le nombre dérivé f′(a)."),
                F("y = f′(a) × (x − a) + f(a)"),
                P("La tangente à la courbe de f au point d'abscisse a a pour équation la formule ci-dessus.")),

            L("l-newton", "c-tle-meca", 1, "Lois de Newton", 13,
                H("Les trois lois de Newton"),
                B("1re loi (inertie) : sans force, ou si les forces se compensent, le centre d'inertie garde un mouvement rectiligne uniforme.",
                  "2e loi : la somme des forces extérieures est égale à la masse multipliée par l'accélération.",
                  "3e loi (actions réciproques) : si A exerce une force sur B, alors B exerce sur A une force opposée."),
                F("ΣF = m × a")),

            L("l-pythagore", "c-3e-geo", 1, "Théorème de Pythagore", 8,
                H("Théorème de Pythagore"),
                Def("Théorème : dans un triangle rectangle, le carré de l'hypoténuse est égal à la somme des carrés des deux autres côtés."),
                F("BC² = AB² + AC²"),
                Ex("Exemple : AB = 3 et AC = 4 donnent BC² = 9 + 16 = 25, donc BC = 5.")),
            L("l-thales", "c-3e-geo", 2, "Théorème de Thalès", 9,
                H("Théorème de Thalès"),
                P("Dans un triangle coupé par une droite parallèle à l'un des côtés, les longueurs des côtés correspondants sont proportionnelles."),
                F("AM / AB = AN / AC = MN / BC")),
            L("l-phrases", "c-3e-gram", 1, "Les types de phrases", 7,
                H("Les quatre types de phrases"),
                B("Déclarative : elle donne une information.", "Interrogative : elle pose une question.", "Impérative : elle exprime un ordre ou un conseil.", "Exclamative : elle exprime une émotion.")),

            L("l-quadratic", "c-f5-alg", 1, "Quadratic equations", 12,
                H("Quadratic equations"),
                P("A quadratic equation has the form ax² + bx + c = 0, where a ≠ 0."),
                Def("Definition: the discriminant is Δ = b² − 4ac. If Δ > 0 there are two real roots, if Δ = 0 there is one double root, and if Δ < 0 there is no real root."),
                F("x = (−b ± √Δ) / 2a"),
                Ex("Example: x² − 5x + 6 = 0 gives Δ = 1, so x = 2 or x = 3.")),
            L("l-reported", "c-f5-gram", 1, "Reported speech", 9,
                H("Reported speech"),
                P("We use reported speech to tell someone what another person said, usually moving the verb one tense back."),
                Tbl(("Direct", "Reported", "Change"), ("\"I am tired.\"", "She said she was tired.", "present → past"), ("\"I will come.\"", "He said he would come.", "will → would"))),
            L("l-kinematics", "c-u6-mech", 1, "Equations of motion", 14,
                H("Equations of uniform acceleration"),
                F("v = u + at"),
                F("s = ut + ½at²"),
                F("v² = u² + 2as"),
                P("These equations apply when the acceleration a is constant along a straight line."))
        };
        db.Lecons.AddRange(lecons);

        // ----- Quiz -----
        var quizzes = new List<Quiz>
        {
            Q("l-suites-arith", "Quiz : suites arithmétiques",
                new QuestionDto("Quelle est la raison de la suite 3 ; 7 ; 11 ; 15 ?", new List<string> { "3", "4", "7", "11" }, 1, "On passe de 3 à 7 en ajoutant 4 : la raison est r = 4."),
                new QuestionDto("Une suite arithmétique de raison négative est...", new List<string> { "Croissante", "Constante", "Décroissante" }, 2, "Ajouter un nombre négatif fait diminuer les termes."),
                new QuestionDto("Si u₀ = 2 et r = 5, combien vaut u₃ ?", new List<string> { "10", "15", "17", "20" }, 2, "u₃ = u₀ + 3r = 2 + 15 = 17.")),
            Q("l-pythagore", "Quiz : Pythagore",
                new QuestionDto("Dans un triangle rectangle de côtés 6 et 8, quelle est l'hypoténuse ?", new List<string> { "10", "12", "14", "7" }, 0, "6² + 8² = 100, donc l'hypoténuse vaut 10."),
                new QuestionDto("Le théorème de Pythagore s'applique à...", new List<string> { "Tout triangle", "Un triangle rectangle", "Un triangle isocèle" }, 1, "Il n'est valable que dans un triangle rectangle.")),
            Q("l-quadratic", "Quiz: quadratic equations",
                new QuestionDto("What is the discriminant of x² − 5x + 6 = 0?", new List<string> { "1", "25", "−1", "49" }, 0, "Δ = (−5)² − 4 × 1 × 6 = 25 − 24 = 1."),
                new QuestionDto("If Δ < 0, the equation has...", new List<string> { "Two real roots", "One double root", "No real root" }, 2, "A negative discriminant means no real solution."))
        };
        db.Quizzes.AddRange(quizzes);
        foreach (var q in quizzes) lecons.First(l => l.Id == q.LeconId).HasQuiz = true;
        await db.SaveChangesAsync();

        // ----- Publication : génération des paquets ZIP (version 1) -----
        foreach (var l in lecons)
        {
            var pkg = await packages.BuildAsync(l, 1);
            l.Checksum = pkg.Checksum;
            l.TailleOctets = pkg.TailleOctets;
            l.Statut = PublicationStatus.Publiee;
            l.EverPublished = true;
            l.PublishedUtc = DateTime.UtcNow;
        }

        if (cfg.GetValue("Seed:DemoEleve", true))
        {
            db.Users.Add(new User
            {
                Prenom = "Pierre", Nom = "Alexandre", Identifiant = "eleve@demo.cm", Role = UserRole.Eleve,
                PasswordHash = PasswordHelper.Hash("Demo1234"), ClasseId = "tle-c", SousSysteme = SousSysteme.Francophone,
                ConsentementUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Données de démonstration créées : {Classes} classes, {Lecons} leçons publiées.", classes.Length, lecons.Count);
    }

    // ----- Fabriques de contenu -----
    private static Lecon L(string id, string chapitre, int ordre, string titre, int min, params LessonBlockDto[] blocs) =>
        new() { Id = id, ChapitreId = chapitre, Ordre = ordre, Titre = titre, DureeMin = min, ContenuJson = Json.ToJson(blocs.ToList()), Statut = PublicationStatus.Brouillon };

    private static Quiz Q(string leconId, string titre, params QuestionDto[] questions) =>
        new() { LeconId = leconId, Titre = titre, QuestionsJson = Json.ToJson(questions.ToList()) };

    private static LessonBlockDto H(string t) => new(BlockType.Heading, t);
    private static LessonBlockDto P(string t) => new(BlockType.Paragraph, t);
    private static LessonBlockDto Def(string t) => new(BlockType.Definition, t);
    private static LessonBlockDto Ex(string t) => new(BlockType.Example, t);
    private static LessonBlockDto F(string t) => new(BlockType.Formula, t);
    private static LessonBlockDto B(params string[] items) => new(BlockType.BulletList, Items: items.ToList());
    private static LessonBlockDto Tbl(params (string, string, string)[] rows) =>
        new(BlockType.Table, Rows: rows.Select(r => new TableRowDto(r.Item1, r.Item2, r.Item3)).ToList());
}
