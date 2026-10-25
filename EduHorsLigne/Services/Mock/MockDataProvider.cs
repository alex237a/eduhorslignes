using EduHorsLigne.Models;

namespace EduHorsLigne.Services.Mock;

/// <summary>
/// Source UNIQUE de données factices pour la maquette interactive.
/// TODO BACKEND : remplacer chaque méthode par un appel aux services de /Services/Interfaces.
/// </summary>
public class MockDataProvider
{
    public Eleve Eleve { get; } = new()
    {
        Prenom = "Pierre", Nom = "Alexandre", SousSysteme = SousSysteme.Francophone, Classe = "Terminale C"
    };

    public IReadOnlyList<Matiere> Matieres { get; } = new List<Matiere>
    {
        new() { Id = "math", Nom = "Mathématiques", Initiale = "M", Icone = "Σ", Ton = "Ok", Meta = "12 chap. · 3 téléchargées", NbLecons = 48, NbTerminees = 37, TailleMo = 52, Progression = 0.78 },
        new() { Id = "pc", Nom = "Physique-Chimie", Initiale = "P", Icone = "⚛", Ton = "Warn", Meta = "10 chap. · 2 téléchargées", NbLecons = 40, NbTerminees = 22, TailleMo = 40, Progression = 0.54 },
        new() { Id = "svt", Nom = "SVT", Initiale = "S", Icone = "🌿", Ton = "Ok", Meta = "8 chap. · 1 téléchargée", NbLecons = 32, NbTerminees = 13, TailleMo = 36, Progression = 0.41 },
        new() { Id = "fr", Nom = "Français", Initiale = "F", Icone = "Aa", Ton = "Neutral", Meta = "9 chap. · 0 téléchargée", NbLecons = 36, NbTerminees = 8, TailleMo = 20, Progression = 0.22, HorsLigneVide = true },
    };

    public Matiere GetMatiere(string? id) => Matieres.FirstOrDefault(m => m.Id == id) ?? Matieres[0];

    private static Lecon L(string chapitre, string titre, int min, double mo, LessonStatus s, double avancement = 0, bool fav = false) => new()
    {
        Titre = titre, ChapitreTitre = chapitre, MatiereNom = "Mathématiques", DureeMin = min, TailleMo = mo, Statut = s, Avancement = avancement, EstFavori = fav
    };

    public IReadOnlyList<Lecon> Lecons { get; } = new List<Lecon>
    {
        L("Chapitre 1 · Limites et continuité", "Notion de limite", 12, 3.4, LessonStatus.Read),
        L("Chapitre 1 · Limites et continuité", "Limites et opérations", 9, 2.1, LessonStatus.InProgress, 0.45),
        L("Chapitre 1 · Limites et continuité", "Continuité en un point", 15, 4.2, LessonStatus.UpdateAvailable),
        L("Chapitre 1 · Limites et continuité", "Théorème des valeurs intermédiaires", 11, 3.8, LessonStatus.NotDownloaded, 0, true),
        L("Chapitre 2 · Dérivation", "Nombre dérivé et tangente", 14, 3.1, LessonStatus.NotDownloaded),
    };

    public IReadOnlyList<LeconGroup> GetGroups(Func<Lecon, bool>? filter = null) =>
        Lecons.Where(l => filter?.Invoke(l) ?? true)
              .GroupBy(l => l.ChapitreTitre)
              .Select((g, i) => new LeconGroup(g.Key, g, i == 0 ? 4 : 5, i == 0))
              .ToList();

    private static Exercice E(int n, string chap, string titre, Difficulty d, int min, double pts, ExerciseStatus s, double? obtenu = null, bool telecharge = true, double mo = 0) => new()
    {
        Numero = n, Titre = titre, ChapitreTitre = chap, MatiereNom = "Mathématiques", Difficulte = d, DureeMin = min, Points = pts,
        Statut = s, PointsObtenus = obtenu, Telecharge = telecharge, TailleMo = mo, Corrige = obtenu is not null
    };

    public IReadOnlyList<Exercice> Exercices { get; } = new List<Exercice>
    {
        E(1, "Limites et continuité", "Théorème des valeurs intermédiaires & bijection", Difficulty.Medium, 25, 4.5, ExerciseStatus.Done, 4),
        E(2, "Limites et continuité", "Calcul de limites indéterminées et asymptotes", Difficulty.Easy, 15, 3, ExerciseStatus.NotStarted),
        E(3, "Limites et continuité", "Continuité uniforme et encadrements", Difficulty.Hard, 35, 5, ExerciseStatus.NotStarted),
        E(1, "Dérivation et primitives", "Étude de fonction rationnelle et point d'inflexion", Difficulty.Medium, 30, 5, ExerciseStatus.NotStarted, null, false, 1.2),
    };

    public IReadOnlyList<LessonBlock> Blocks { get; } = new List<LessonBlock>
    {
        new() { Type = BlockType.Heading, Text = "Suites arithmétiques" },
        new() { Type = BlockType.Paragraph, Text = "Une suite arithmétique est une suite de nombres où l'on passe d'un terme au suivant en ajoutant toujours le même nombre." },
        new() { Type = BlockType.Definition, Text = "Définition : une suite (uₙ) est arithmétique s'il existe un réel r, appelé raison, tel que pour tout entier n : uₙ₊₁ = uₙ + r." },
        new() { Type = BlockType.Formula, Text = "uₙ = u₀ + n × r" },
        new() { Type = BlockType.Example, Text = "Exemple : la suite de premier terme u₀ = 3 et de raison r = 4 donne 3 ; 7 ; 11 ; 15 ; ..." },
        new() { Type = BlockType.Heading, Text = "Propriétés" },
        new() { Type = BlockType.BulletList, Items = new() { "Si r > 0, la suite est strictement croissante.", "Si r < 0, la suite est strictement décroissante.", "Si r = 0, la suite est constante." } },
        new() { Type = BlockType.Table, Rows = new()
            {
                new() { C1 = "Raison r", C2 = "Sens de variation", C3 = "Exemple" },
                new() { C1 = "r > 0", C2 = "Croissante", C3 = "2 ; 5 ; 8" },
                new() { C1 = "r < 0", C2 = "Décroissante", C3 = "9 ; 6 ; 3" },
            } },
        new() { Type = BlockType.Heading, Text = "Somme des premiers termes" },
        new() { Type = BlockType.Formula, Text = "S = (n + 1) × (u₀ + uₙ) / 2" },
        new() { Type = BlockType.Paragraph, Text = "Cette formule permet de calculer rapidement la somme de termes consécutifs sans les additionner un par un." },
    };

    public IReadOnlyList<string> Sections { get; } = new List<string> { "Suites arithmétiques", "Propriétés", "Somme des premiers termes" };

    public IReadOnlyList<Question> QuizQuestions { get; } = new List<Question>
    {
        new() { Enonce = "Quelle est la raison de la suite 3 ; 7 ; 11 ; 15 ?", Options = new() { "3", "4", "7", "11" }, IndexBonneReponse = 1,
                Explication = "On passe de 3 à 7 en ajoutant 4 : la raison est r = 4." },
        new() { Enonce = "Une suite arithmétique de raison négative est...", Options = new() { "Croissante", "Constante", "Décroissante" }, IndexBonneReponse = 2,
                Explication = "Ajouter un nombre négatif fait diminuer les termes." },
        new() { Enonce = "Si u₀ = 2 et r = 5, combien vaut u₃ ?", Options = new() { "10", "15", "17", "20" }, IndexBonneReponse = 2,
                Explication = "u₃ = u₀ + 3r = 2 + 15 = 17." },
    };

    public IReadOnlyList<QuestionExercice> ExerciseQuestions { get; } = new List<QuestionExercice>
    {
        new() { Type = QuestionType.SingleChoice, Enonce = "Quelle est la raison de la suite 5 ; 9 ; 13 ; 17 ?",
                Options = new() { "3", "4", "5", "9" }, Indices = new() { "Calculez la différence entre deux termes consécutifs." } },
        new() { Type = QuestionType.MultipleChoice, Enonce = "Parmi ces suites, lesquelles sont arithmétiques ? (plusieurs réponses)",
                Options = new() { "2 ; 4 ; 6 ; 8", "1 ; 2 ; 4 ; 8", "10 ; 7 ; 4 ; 1", "1 ; 4 ; 9 ; 16" }, Indices = new() { "Vérifiez si la différence est constante." } },
        new() { Type = QuestionType.TrueFalse, Enonce = "Une suite arithmétique de raison 0 est constante.", Options = new() { "Vrai", "Faux" } },
        new() { Type = QuestionType.FillBlank, Enonce = "Dans une suite arithmétique, uₙ₊₁ = uₙ + ______ .", Indices = new() { "Le nombre ajouté à chaque étape porte un nom particulier." } },
        new() { Type = QuestionType.ShortAnswer, Enonce = "Calculez u₁₀ si u₀ = 4 et r = 3.", Indices = new() { "Utilisez uₙ = u₀ + n × r." } },
        new() { Type = QuestionType.Essay, Enonce = "Démontrez que la somme S = 1 + 2 + ... + n est égale à n(n + 1)/2.", Indices = new() { "Écrivez la somme dans les deux sens puis additionnez." } },
    };

    public IReadOnlyList<ResultItem> ResultItems { get; } = new List<ResultItem>
    {
        new() { Numero = 1, Enonce = "Quelle est la raison de la suite 5 ; 9 ; 13 ; 17 ?", Reponse = "4", BonneReponse = "4", Explication = "9 − 5 = 4.", Verdict = Verdict.Correct },
        new() { Numero = 2, Enonce = "Quelles suites sont arithmétiques ?", Reponse = "2 ; 4 ; 6 ; 8", BonneReponse = "2 ; 4 ; 6 ; 8 et 10 ; 7 ; 4 ; 1", Explication = "Les deux ont une différence constante (+2 et −3).", Verdict = Verdict.Partial },
        new() { Numero = 3, Enonce = "Une suite de raison 0 est constante.", Reponse = "Faux", BonneReponse = "Vrai", Explication = "Avec r = 0, uₙ₊₁ = uₙ : tous les termes sont égaux.", Verdict = Verdict.Incorrect },
        new() { Numero = 4, Enonce = "Complétez : uₙ₊₁ = uₙ + ___", Reponse = "r", BonneReponse = "r (la raison)", Explication = "Le terme ajouté est la raison r.", Verdict = Verdict.Correct },
        new() { Numero = 5, Enonce = "Calculez u₁₀ si u₀ = 4 et r = 3.", Reponse = "34", BonneReponse = "34", Explication = "u₁₀ = 4 + 10 × 3 = 34.", Verdict = Verdict.Correct },
    };

    public IReadOnlyList<AttemptItem> Attempts { get; } = new List<AttemptItem>
    {
        new() { Date = "12 oct. 2026", Score = "12/20", Duree = "22 min" },
        new() { Date = "05 oct. 2026", Score = "9/20", Duree = "27 min" },
    };

    public IReadOnlyList<LibraryItem> GetLibrary(int tab) => tab switch
    {
        0 => new List<LibraryItem>
        {
            new() { Icone = "⭐", Titre = "Généralités sur les suites", SousTitre = "Suites numériques", Matiere = "Mathématiques" },
            new() { Icone = "⭐", Titre = "Suites arithmétiques", SousTitre = "Suites numériques", Matiere = "Mathématiques" },
            new() { Icone = "⭐", Titre = "Lois de Newton", SousTitre = "Mécanique", Matiere = "Physique-Chimie" },
        },
        1 => new List<LibraryItem>
        {
            new() { Icone = "📝", Titre = "Retenir : uₙ = u₀ + n×r", SousTitre = "Suites arithmétiques · modifiée le 14 oct.", Matiere = "Mathématiques" },
            new() { Icone = "📝", Titre = "Revoir la démonstration du théorème", SousTitre = "Limites · modifiée le 10 oct.", Matiere = "Mathématiques" },
            new() { Icone = "📝", Titre = "Unités des grandeurs en mécanique", SousTitre = "Lois de Newton · modifiée le 08 oct.", Matiere = "Physique-Chimie" },
        },
        2 => new List<LibraryItem>
        {
            new() { Icone = "🔖", Titre = "Définition de la raison", SousTitre = "Suites arithmétiques · section 1", Matiere = "Mathématiques" },
            new() { Icone = "🔖", Titre = "Formule de la somme", SousTitre = "Suites arithmétiques · section 3", Matiere = "Mathématiques" },
        },
        _ => new List<LibraryItem>
        {
            new() { Icone = "🔁", Titre = "Une suite de raison 0 est constante.", SousTitre = "Suites arithmétiques : applications", Matiere = "Mathématiques" },
            new() { Icone = "🔁", Titre = "Calculez la limite en +∞ de (2x+1)/(x−3)", SousTitre = "Calcul de limites", Matiere = "Mathématiques" },
        },
    };

    public IReadOnlyList<SearchResult> SearchResults { get; } = new List<SearchResult>
    {
        new() { Titre = "Théorème des valeurs intermédiaires", Avant = "…le ", Mot = "théorème", Apres = " des valeurs intermédiaires assure l'existence d'au moins une solution c dans l'intervalle…",
                Source = "Mathématiques · Chapitre 1 · Section 3", SourceIcone = "Σ", Etiquette = "Lue", EtiquetteIcone = "✔", EtiquetteTon = "Ok" },
        new() { Titre = "Théorème de Thalès", Avant = "…dans un triangle coupé par une droite parallèle, le ", Mot = "théorème", Apres = " permet d'établir les rapports de proportionnalité…",
                Source = "Mathématiques · 3e · Chapitre 4", SourceIcone = "∧", Etiquette = "Lue", EtiquetteIcone = "✔", EtiquetteTon = "Ok" },
        new() { Titre = "Théorème de Pythagore", Avant = "…le célèbre ", Mot = "théorème", Apres = " relie les carrés des longueurs des côtés de l'angle droit à l'hypoténuse…",
                Source = "Mathématiques · 4e · Chapitre 2", SourceIcone = "◺", Etiquette = "Cours", EtiquetteIcone = "📖", EtiquetteTon = "Neutral" },
        new() { Titre = "Ma note : Réviser les théorèmes", Avant = "…revoir avant l'épreuve blanche tous les ", Mot = "théorèmes", Apres = " d'analyse et préparer les fiches mémo…",
                Source = "Note personnelle · 14/09/2026", SourceIcone = "🔖", Etiquette = "Note", EtiquetteIcone = "📝", EtiquetteTon = "Info" },
        new() { Titre = "Théorème de la limite monotone", Avant = "…toute suite croissante et majorée converge. Ce ", Mot = "théorème", Apres = " fondamental s'applique également aux suites récurrentes…",
                Source = "Mathématiques · Chapitre 2", SourceIcone = "↗", Etiquette = "À télécharger", EtiquetteIcone = "⬇", EtiquetteTon = "Warn" },
    };

    public IReadOnlyList<string> SearchHistory { get; } = new List<string> { "théorème", "suites", "dérivée" };

    public IReadOnlyList<StorageItem> StorageItems { get; } = new List<StorageItem>
    {
        new() { Nom = "Mathématiques", TailleMo = 52, Fraction = 0.52 },
        new() { Nom = "Physique-Chimie", TailleMo = 40, Fraction = 0.40 },
        new() { Nom = "SVT", TailleMo = 36, Fraction = 0.36 },
        new() { Nom = "Français", TailleMo = 20, Fraction = 0.20 },
        new() { Nom = "Anglais", TailleMo = 18, Fraction = 0.18 },
    };

    public IReadOnlyList<FaqItem> Faq { get; } = new List<FaqItem>
    {
        new() { Question = "Comment télécharger une leçon ?", Reponse = "Ouvrez une matière, choisissez une leçon, un chapitre ou la matière entière, puis appuyez sur « Télécharger » et confirmez." },
        new() { Question = "Puis-je lire sans Internet ?", Reponse = "Oui. Une fois téléchargées, vos leçons, notes, marque-pages et exercices restent disponibles hors ligne." },
        new() { Question = "Que devient mon travail si je supprime une leçon ?", Reponse = "Vos notes, marque-pages, favoris et votre progression sont conservés ; seul le contenu de la leçon est effacé." },
        new() { Question = "Comment économiser mes données mobiles ?", Reponse = "Activez « Télécharger uniquement en Wi-Fi » dans les paramètres." },
        new() { Question = "Comment signaler une erreur dans une leçon ?", Reponse = "Depuis le lecteur ou les paramètres, utilisez « Signaler une erreur ». Le message sera envoyé dès que vous aurez Internet." },
    };
}
