# ÉduHors-Ligne : backend (API REST)

API **ASP.NET Core (.NET 10) + EF Core + SQLite + JWT** qui sert l'application MAUI *offline-first* :
authentification, catalogue, téléchargement de leçons (paquets ZIP vérifiables), administration du contenu,
synchronisation minimale. Elle correspond au plan « backend minimal pour la démo ».

## Lancer sur Windows

1. Prérequis : SDK .NET 10 (`dotnet --version` → 10.x) et accès Internet au premier lancement (restauration des packages depuis nuget.org).
2. **Visual Studio** : ouvrir `EduHorsLigne.Api.csproj`, choisir le profil **EduHorsLigne.Api (Windows)** dans la barre d'outils (pas « IIS »), puis F5. Le navigateur s'ouvre sur `/admin/`.
3. **Sans Visual Studio** : double-cliquer sur `run-windows.bat`.
4. Pour tester depuis un téléphone : profil « réseau local » (écoute sur `0.0.0.0:5080`) et autoriser le port 5080 dans le pare-feu Windows.

> Si l'erreur « Package … introuvable » apparaît : le fichier `NuGet.config` du projet ajoute nuget.org comme source. Si vous la voyez encore,
> vérifiez dans Visual Studio *Outils > Options > Gestionnaire de package NuGet > Sources de package* que **nuget.org** est coché,
> puis supprimez les dossiers `bin` et `obj` et relancez.

## Lancer (ligne de commande)

```bash
cd EduHorsLigne.Api
dotnet run            # http://0.0.0.0:5080 ; la base SQLite et les données de démo sont créées au premier démarrage
```

| Quoi | Où |
|---|---|
| Interface d'administration | http://localhost:5080/admin/ |
| Compte admin | `admin@eduhorsligne.cm` / `Admin#2026!` (à changer : `appsettings.json` → `Seed`) |
| Compte élève de démo | `eleve@demo.cm` / `Demo1234` (classe Terminale C) |
| Requêtes d'essai | `EduHorsLigne.Api.http` (VS / VS Code « REST Client ») |
| Test de bout en bout | `./scripts/smoke-test.sh` (API démarrée) |

Depuis l'**émulateur Android**, l'API est à `http://10.0.2.2:5080`. Depuis un téléphone réel : `http://<IP du PC>:5080`
(même Wi-Fi, pare-feu ouvert). Le HTTPS est exigé par le cahier des charges en production (reverse proxy ou certificat Kestrel).

## Endpoints

| Module | Endpoints | Exigences |
|---|---|---|
| Auth élève | `POST /api/auth/register`, `/login`, `/forgot-password`, `/verify-code`, `/reset-password` | EF-79 à 86 |
| Profil | `GET/PUT/DELETE /api/me` | EF-04, EF-88 |
| Catalogue (public) | `GET /api/classes`, `/classes/{id}/subjects`, `/subjects/{id}/chapters`, `/chapters/{id}/lessons`, `/lessons/{id}`, **`/catalog?classeId=`** (arbre complet pour le hors ligne) | EF-08 à 14, RG8 |
| Téléchargement | `GET /api/lessons/{id}/package`, `/lessons/{id}/checksum`, `/chapters/{id}/package`, `/subjects/{id}/package`, `POST /lessons/package-batch` | EF-15, 17, 22 |
| Mises à jour | `POST /api/lessons/check-updates` (`[{id, version}]` → `MiseAJour` / `Archivee`) | EF-23, 60, 62, RG6 |
| Retours | `POST /api/lessons/{id}/reports`, `/rating` | EF-70, 71 |
| Synchronisation | `POST /api/sync` (push + pull, dernière modification gagnante) | EF-63 |
| Admin | `POST /api/admin/login`, CRUD `classes`, `subjects`, `chapters`, `lessons`, `submit-review`, `publish`, `unpublish`, `archive`, `package` (upload ZIP), `quiz`, `stats`, `reports` | EF-72 à 78, CU5 |
| Système | `GET /api/health`, `GET /api/version` | |

Erreurs : toujours `{ "code", "message", "fields"?, "tentativesRestantes"?, "retryAfterSeconds"? }`, ce qui permet d'afficher
le message sous le champ concerné (EF-81) et « il reste N tentatives » (EF-86).

## Points de conception

- **Paquet de leçon** : ZIP déterministe contenant `manifest.json`, `lesson.json` (blocs + sections pour le sommaire), `quiz.json`.
  Somme de contrôle **SHA-256** dans l'en-tête `X-Checksum-Sha256`, l'`ETag` et `/checksum`. Les requêtes **HTTP Range** sont gérées :
  un téléchargement coupé reprend où il s'est arrêté (EF-17). Les images peuvent être ajoutées via un paquet ZIP téléversé.
- **Lots** : chapitre, matière ou sélection libre → un ZIP de ZIP + `manifest.json` (checksum par leçon).
- **Versions (CU5)** : la modification d'une leçon publiée n'affecte pas les élèves tant qu'elle n'est pas republiée.
  1re publication = v1 ; chaque republication incrémente la version, détectée par `check-updates`.
- **Cycle de vie** : Brouillon → EnRelecture → Publiée → Archivée. Archivée : plus téléchargeable, reste lisible chez l'élève (RG6).
- **Sécurité** : mots de passe PBKDF2-SHA256 salés ; JWT de 30 jours (session active hors ligne, EF-84) ; blocage 15 min après 5 échecs ;
  code de réinitialisation à 6 chiffres, haché, valable 10 min, 5 essais ; réponse identique que le compte existe ou non ;
  les administrateurs n'ont aucun endpoint donnant accès aux notes des élèves (RG9).
- **Code de réinitialisation simulé** : écrit dans les journaux (`IResetCodeSender`) ; renvoyé dans la réponse (`devCode`) seulement si
  `Auth:ExposeResetCode = true` (activé en Development). Pour un vrai SMS/e-mail, implémenter `IResetCodeSender`.
- **Synchronisation** : identifiants de notes et marque-pages générés par l'application (GUID) pour créer hors ligne ; favoris et
  progression ont pour clé (élève, leçon) ; suppressions logiques ; horloge client plafonnée à « maintenant + 5 min ».

## Le front MAUI est déjà branché

Dans la solution complète (`EduHorsLigne.sln`), l'application utilise cette API pour l'authentification, le catalogue, le téléchargement,
la lecture hors ligne et la synchronisation. Voir `README-LANCEMENT.md` à la racine.

## Hors périmètre de cette première version

Exercices et annales (le front les simule encore), vrai envoi SMS/e-mail, mises à jour différentielles, rôles d'administration fins,
enseignant contributeur, partage Bluetooth, notifications push, migrations EF Core (la base est créée par `EnsureCreated`),
tests automatisés (seul le script `smoke-test.sh` existe).

## Passer à PostgreSQL / déployer

Remplacer le package `Microsoft.EntityFrameworkCore.Sqlite` par `Npgsql.EntityFrameworkCore.PostgreSQL`, `UseSqlite` par `UseNpgsql`,
générer des migrations (`dotnet ef migrations add Init`), définir `Jwt:Key` (≥ 32 caractères, hors dépôt), changer le mot de passe admin,
servir derrière HTTPS et déplacer `Storage/` vers un volume persistant (ou S3/MinIO).
