# ÉduHors-Ligne : front-end .NET MAUI (maquette interactive)

Front-end XAML complet (23 écrans) de l'application ÉduHors-Ligne, en **.NET 10 / MAUI**, cible **Android 8.0+**.
Seule la **navigation** est active ; les données sont factices (`Services/Mock/MockDataProvider.cs`).
Le projet est structuré pour recevoir le backend C# sans toucher aux Views (voir « Brancher le backend »).

## 1. Prérequis

| Élément | Version |
|---|---|
| SDK .NET | 10.0 (`dotnet --version` doit afficher 10.x) |
| Workload MAUI | `dotnet workload install maui` (terminal administrateur) |
| Android | SDK Android + JDK (installés par Visual Studio 2026 ou par `dotnet build -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=True`) |
| Appareil | Émulateur Android (API 26 ou plus, ex. Pixel 7 API 35) ou téléphone en mode développeur (USB) |

## 2. Lancer le projet

**Visual Studio 2026 (recommandé)**
1. Ouvrir `EduHorsLigne.csproj`.
2. Choisir un émulateur Android (Device Manager > créer un Pixel API 35 si besoin) dans la barre d'outils.
3. Appuyer sur **F5**.

**Ligne de commande** (émulateur démarré ou téléphone branché)
```
cd EduHorsLigne
dotnet restore
dotnet build -t:Run -f net10.0-android
```

**Visual Studio Code** : extension « .NET MAUI », ouvrir le dossier, choisir « Run > Android ».

**Si la restauration ou la compilation échoue** (versions de packages, workload) : créer un projet vierge
`dotnet new maui -n EduHorsLigne`, puis copier dans ce projet les dossiers `Models`, `ViewModels`, `Views`, `Services`,
`Helpers`, `Resources/Styles`, `Resources/Strings`, `Platforms/Android` ainsi que `App.xaml(.cs)`, `AppShell.xaml(.cs)`,
`MauiProgram.cs`, et ajouter le package `CommunityToolkit.Mvvm` (8.4.0). Supprimer les fichiers d'exemple (`MainPage`).

## 2 bis. Lancer avec « Windows Machine » (sans émulateur)

Le projet cible **Windows** (`net10.0-windows10.0.19041.0`) en premier sous Windows : « Windows Machine » est donc la cible par défaut.
1. Windows : *Paramètres > Confidentialité et sécurité > Pour les développeurs > Mode développeur : activé*.
2. Visual Studio 2026 avec la charge de travail **MAUI** (elle inclut le SDK Windows et le Windows App SDK).
3. Ouvrir `EduHorsLigne.csproj`, vérifier que la liste du bouton ▶ affiche **Windows Machine**, puis F5.
4. Ligne de commande : `dotnet build -t:Run -f net10.0-windows10.0.19041.0`.

**Sans le workload Android** (si l'installation Android n'est pas faite) : compiler en Windows seul avec
`dotnet build -t:Run -f net10.0-windows10.0.19041.0 -p:IncludeAndroid=false`, ou passer `IncludeAndroid` à `false` dans le `.csproj`.

**Si `mt.exe` échoue (code 31) ou si la compilation Windows se comporte bizarrement** :
1. fermer Visual Studio, supprimer les dossiers `bin` et `obj` du projet, rouvrir et recompiler ;
2. déplacer le projet hors de `Images` / `Documents` / `Bureau` (dossiers parfois protégés ou synchronisés par OneDrive) vers un chemin simple, par exemple `C:\Dev\EduHorsLigne` ;
3. vérifier que *Sécurité Windows > Protection contre les virus > Accès contrôlé aux dossiers* ne bloque pas Visual Studio.

La fenêtre s'ouvre à la taille d'un téléphone (430 x 820) et reste redimensionnable : tirer sur les bords pour tester le responsive
(Compact, Medium, Expanded).

## 3. Correspondance avec tes maquettes (PDF « EduHorsLigne »)

| Page du PDF | Écran dans l'application | Comment y arriver |
|---|---|---|
| 3 · « Révisez n'importe où » | **WelcomePage** (premier écran) | Au lancement |
| 2 · Connexion | **LoginPage** | « J'ai déjà un compte » |
| 12 · Connexion en erreur / en cours | **LoginPage** (états) | Mot de passe `wrong` = erreur + tentatives restantes ; identifiant mal formé = « Invalide » ; bouton « Connexion en cours… » pendant l'envoi |
| 11 et 15 · Inscription | **RegisterPage** (états normal et erreurs) | « Créer un compte élève » ; taper `x`, un mot de passe faible, une confirmation différente |
| 13 · Vérification du code | **OtpPage** | Connexion > « Mot de passe oublié ? » > identifiant > code + nouveau mot de passe |
| 4 · Accueil vide | **HomePage** (état Vide) | Profil > Paramètres > Mode démo > **Vide** |
| 16 · Accueil | **HomePage** | Connexion (barre inférieure : Accueil, Recherche, Bibliothèque, Progression, Profil) |
| 5 · Matière Mathématiques | **SubjectPage** | Accueil > carte « Mathématiques » |
| 7 · Détail leçon + « Confirmer le téléchargement » | **LessonDetailPage** | Matière > une leçon (la feuille s'ouvre pour une leçon non téléchargée, ou bouton « Télécharger ») |
| 9 · Lecteur clair / 6 · Lecteur sombre | **ReaderPage** | Accueil > « Continuer ma lecture » ; bouton **Aa** > « Mode sombre » |
| 10 · Recherche « théorème » | **SearchPage** | Onglet Recherche |
| 14 · Exercices et Annales | **ExercisesHomePage** | Matière > bannière « Programme officiel », ou Détail leçon > Exercices |
| 8 · Exercices & Corrigés | **ExerciseCorrectionPage** | Exercices > une ligne « Corrigé » (onglets Énoncé / Corrigé / Méthodologie) |
| 17 · Session exercice | **ExerciseSessionPage** | Exercices > « Reprendre la session » ou une ligne non commencée |
| 18 · Correction Annales | **ExerciseResultPage** | Session > « Valider & Suivante » |

Écrans complémentaires (hors maquette, même charte) : Bibliothèque, Progression, Profil, Paramètres, Stockage, Aide/FAQ,
Quiz, Mot de passe oublié, Galerie des états.

Comportements démo : mot de passe `wrong` pour simuler l'erreur ; langue FR/EN à chaud et thème sombre dans Paramètres ;
recherche insensible aux accents (« theoreme »).

## 4. Schéma de navigation

```
Welcome ──► Inscription ──► (app)           Welcome ──► Connexion ──► (app)
   │                                                 ├─► Mot de passe oublié ► Code + nouveau mot de passe ► Connexion
   └─► mode invité ► (app)                           └─► mode invité ► (app)

Application (barre inférieure, 5 destinations)
 ├─ Accueil ► Matière ► Détail de leçon (feuille de téléchargement) ► Lecteur (clair/sombre) ► Quiz
 │            │                                      └► Exercices et Annales
 │            └► Exercices et Annales ► Corrigé pas à pas
 │                                    ► Session ► Correction ► Corrigé / Exercices
 ├─ Recherche ► Lecteur            ├─ Bibliothèque ► Lecteur
 ├─ Progression ► Matière          └─ Profil ► Paramètres (Aide, Galerie) · Stockage · Aide · Déconnexion
```

## 5. Responsive (classes Compact < 600 dp, Medium 600-840, Expanded > 840)

- Aucune largeur fixe de conteneur ; formulaires et lecteurs centrés (`CenteredContent`, 480 à 720 dp).
- `ResponsiveSpanBehavior` : cartes de matières en 2 / 3 / 4 colonnes selon la largeur réelle.
- Lecteur et exercices limités en largeur sur grand écran ; barre inférieure à 5 onglets.
- Texte : tailles système (suit la police Android) + taille réglable dans le lecteur ; zones tactiles >= 48 dp.
- Sous Windows, tirer sur les bords de la fenêtre (430 x 820 au démarrage) pour passer de Compact à Expanded.

## 6. Structure

```
Models/        entités POCO (Id, Version, dates UTC)
ViewModels/    un ViewModel par écran (CommunityToolkit.Mvvm), commandes métier marquées // TODO BACKEND
Views/         pages XAML (compiled bindings) + Views/Controls (AppBar, FormField, ChipBar, Badge, IconCircle, EmptyState, StateOverlay, cartes...)
Services/      Interfaces (contrats du backend), NavigationService, DialogService, Mock/MockDataProvider
Helpers/       Loc (FR/EN à chaud, liaison {Binding [cle], Source={x:Static loc:Loc.Instance}}), ResponsiveSpanBehavior, ScreenInfo, Demo (actions « à venir »)
Resources/     Styles (charte unique clair/sombre), Strings (.resx FR/EN, 486 clés), Images (4 photos extraites de tes maquettes, ~100 Ko au total)
```

## 7. Brancher le backend

1. Implémenter les interfaces de `Services/Interfaces/Interfaces.cs` (`ICatalogueService`, `IAuthService`, `IDownloadService`...).
2. Les enregistrer dans `MauiProgram.cs`.
3. Dans chaque ViewModel, remplacer l'usage de `MockDataProvider` par le service correspondant et compléter les commandes
   marquées `// TODO BACKEND`. Les XAML n'ont pas à changer.

Rechercher `TODO BACKEND` dans la solution pour obtenir la liste complète.

## 8. Limites connues de cette maquette

- Aucun téléchargement, aucune persistance, aucune authentification réelle ; les boutons métier affichent « Fonction à venir ».
- Les icônes sont des emojis / symboles système (aucune police à télécharger : écoconception). Les pictogrammes Material des maquettes sont donc approchés.
- La police des maquettes (Public Sans) est remplacée par la police système, conformément au cahier des charges (§16.6).
- Changer la langue ne retraduit pas les listes déroulantes déjà affichées (elles se retraduisent à la prochaine ouverture).
- Les contenus factices (leçons, exercices) sont en français.
