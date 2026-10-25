# ÉduHors-Ligne : lancer l'application ET le serveur

L'**application** (MAUI) et le **serveur** (API + administration web) sont deux programmes distincts.
Pour que l'application fonctionne, le serveur doit tourner. Au démarrage de l'API, un navigateur ne s'ouvre que sur
l'administration : c'est normal, l'application se lance séparément.

## Méthode 1 : un seul double-clic
`start-all.bat` démarre l'API dans une fenêtre, attend qu'elle réponde, puis compile et lance l'application Windows (`run-app.bat`).
`run-app.bat` seul relance uniquement l'application (l'API doit déjà tourner).

## Si l'application se ferme aussitôt
`run-app.bat` affiche à la fermeture le journal `startup.log`, situé à côté de `EduHorsLigne.exe`
(`EduHorsLigne\bin\Debug\net10.0-windows10.0.19041.0\win-x64\`). Il indique jusqu'où le démarrage est allé et l'exception éventuelle.
Envoyez-en le contenu pour un diagnostic précis.

## Méthode 2 : Visual Studio
1. Ouvrir `EduHorsLigne.sln`.
2. Clic droit sur la solution > **Configurer les projets de démarrage…** > **Plusieurs projets de démarrage** :
   `EduHorsLigne.Api` = **Démarrer**, `EduHorsLigne` = **Démarrer** (l'API doit être placée **avant** l'application).
3. Pour `EduHorsLigne.Api`, choisir le profil **EduHorsLigne.Api (Windows)** ; pour `EduHorsLigne`, la cible **Windows Machine**.
4. F5.

## Comptes de démonstration
| Rôle | Identifiant | Mot de passe |
|---|---|---|
| Élève (Terminale C) | `eleve@demo.cm` | `Demo1234` |
| Administrateur (http://localhost:5080/admin/) | `admin@eduhorsligne.cm` | `Admin#2026!` |

Vous pouvez aussi vous inscrire : l'écran d'inscription charge les classes depuis le serveur.
« Mot de passe oublié » : le code à 6 chiffres est affiché dans une boîte de dialogue (SMS simulé en développement).

## Scénario de démonstration
1. Se connecter (internet requis la première fois) → l'accueil affiche les matières de la classe.
2. Ouvrir une matière → **Télécharger** une leçon ou « Tout » (taille et confirmation, somme de contrôle vérifiée).
3. Couper le serveur (ou Internet) → **Lire** la leçon : elle s'ouvre intégralement hors ligne ; la position de lecture est mémorisée.
4. Dans l'administration : modifier une leçon puis **Publier** → relancer l'application : la leçon passe en « Version 2 disponible ».
5. Se déconnecter / reconnecter : favoris et progression sont synchronisés.

## Adresse du serveur
`EduHorsLigne/Services/Api/ApiSettings.cs` : `http://localhost:5080` (Windows) ou `http://10.0.2.2:5080` (émulateur Android).
Pour un **téléphone réel** : mettre `http://<IP-du-PC>:5080`, démarrer l'API avec le profil « réseau local » et autoriser le port 5080 dans le pare-feu.

## Ce qui est relié au serveur / ce qui reste simulé
| Relié au serveur | Encore simulé (données de maquette) |
|---|---|
| Inscription, connexion, mot de passe oublié, déconnexion, session hors ligne | Recherche plein texte |
| Catalogue (cache hors ligne), accueil, matières, détail de leçon | Bibliothèque (notes, marque-pages) |
| Téléchargement avec reprise + SHA-256, lecteur hors ligne | Quiz et exercices |
| Favoris, progression locale + synchronisation, mises à jour de version | Écran de stockage, objectifs, statistiques d'étude |
