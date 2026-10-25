#!/usr/bin/env bash
# Test de bout en bout du scénario de démo. Usage : ./scripts/smoke-test.sh [http://localhost:5080]
# Prérequis : API démarrée (dotnet run), curl, sha256sum. Les données de démonstration doivent être présentes.
set -euo pipefail
BASE="${1:-http://localhost:5080}"
ok()   { echo "  ✔ $1"; }
fail() { echo "  ✘ $1"; exit 1; }
tok()  { sed -E 's/.*"token":"([^"]+)".*/\1/'; }
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

echo "1. Santé"
curl -fsS "$BASE/api/health" | grep -q '"status":"ok"' && ok "GET /api/health"

echo "2. Inscription et connexion élève"
ID="test$RANDOM@demo.cm"
REG=$(curl -fsS -X POST "$BASE/api/auth/register" -H 'Content-Type: application/json' \
  -d "{\"nom\":\"Test\",\"prenom\":\"Eleve\",\"identifiant\":\"$ID\",\"motDePasse\":\"Secret123\",\"confirmationMotDePasse\":\"Secret123\",\"classeId\":\"tle-c\",\"accepteConditions\":true}")
TOKEN=$(echo "$REG" | tok); [ -n "$TOKEN" ] && ok "inscription → JWT reçu"
curl -fsS -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d "{\"identifiant\":\"$ID\",\"motDePasse\":\"Secret123\"}" | grep -q token && ok "connexion"
CODE=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' -d "{\"identifiant\":\"$ID\",\"motDePasse\":\"mauvais\"}")
[ "$CODE" = "401" ] && ok "mauvais mot de passe → 401"
H="Authorization: Bearer $TOKEN"

echo "3. Catalogue"
curl -fsS "$BASE/api/classes" | grep -q 'tle-c' && ok "GET /api/classes"
curl -fsS "$BASE/api/classes/tle-c/subjects" | grep -q 'Mathématiques' && ok "matières de Terminale C"
curl -fsS "$BASE/api/catalog?classeId=tle-c" | grep -q 'Suites arithmétiques' && ok "catalogue complet"

echo "4. Téléchargement d'une leçon + vérification de la somme de contrôle"
curl -fsS -D "$TMP/h" -H "$H" -o "$TMP/l.zip" "$BASE/api/lessons/l-suites-arith/package"
EXPECT=$(tr -d '\r' < "$TMP/h" | grep -i '^x-checksum-sha256:' | awk '{print $2}')
ACTUAL=$(sha256sum "$TMP/l.zip" | awk '{print $1}')
[ "$EXPECT" = "$ACTUAL" ] && ok "SHA-256 identique ($ACTUAL)" || fail "somme de contrôle différente"
SIZE=$(stat -c %s "$TMP/l.zip")
curl -fsS -H "$H" -H "Range: bytes=$((SIZE/2))-" -o "$TMP/part2" "$BASE/api/lessons/l-suites-arith/package"
head -c $((SIZE/2)) "$TMP/l.zip" > "$TMP/resumed.zip"; cat "$TMP/part2" >> "$TMP/resumed.zip"
[ "$(sha256sum "$TMP/resumed.zip" | awk '{print $1}')" = "$ACTUAL" ] && ok "reprise du téléchargement (Range)" || fail "reprise KO"
curl -fsS -H "$H" -o "$TMP/chap.zip" "$BASE/api/chapters/c-tle-lim/package" && ok "lot : chapitre entier"
curl -fsS -H "$H" -o "$TMP/mat.zip" "$BASE/api/subjects/tle-c-math/package" && ok "lot : matière entière"

echo "5. Administration : modifier, republier, détecter la mise à jour"
ATOK=$(curl -fsS -X POST "$BASE/api/admin/login" -H 'Content-Type: application/json' -d '{"identifiant":"admin@eduhorsligne.cm","motDePasse":"Admin#2026!"}' | tok)
AH="Authorization: Bearer $ATOK"
curl -fsS -X PUT "$BASE/api/admin/lessons/l-suites-arith" -H "$AH" -H 'Content-Type: application/json' \
  -d '{"blocs":[{"type":"Heading","text":"Suites arithmétiques (v2)"},{"type":"Paragraph","text":"Contenu corrigé."}]}' >/dev/null && ok "modification du brouillon"
curl -fsS -X POST "$BASE/api/admin/lessons/l-suites-arith/publish" -H "$AH" -H 'Content-Type: application/json' -d '{"noteVersion":"correction"}' | grep -q '"version":2' && ok "publication → version 2"
curl -fsS -X POST "$BASE/api/lessons/check-updates" -H "$H" -H 'Content-Type: application/json' -d '[{"id":"l-suites-arith","version":1}]' | grep -q MiseAJour && ok "l'application détecte la mise à jour"
CODE=$(curl -s -o /dev/null -w '%{http_code}' -H "$H" "$BASE/api/admin/stats"); [ "$CODE" = "403" ] && ok "un élève n'accède pas à l'administration (403)"

echo "6. Synchronisation (dernière modification gagnante)"
NOW=$(date -u +%Y-%m-%dT%H:%M:%SZ); OLD="2020-01-01T00:00:00Z"
curl -fsS -X POST "$BASE/api/sync" -H "$H" -H 'Content-Type: application/json' \
  -d "{\"notes\":[{\"id\":\"n1\",\"leconId\":\"l-suites-arith\",\"position\":0.2,\"texte\":\"Version récente\",\"updatedUtc\":\"$NOW\",\"isDeleted\":false}]}" | grep -q '"appliques":1' && ok "note créée"
curl -fsS -X POST "$BASE/api/sync" -H "$H" -H 'Content-Type: application/json' \
  -d "{\"notes\":[{\"id\":\"n1\",\"leconId\":\"l-suites-arith\",\"position\":0.2,\"texte\":\"Version ancienne\",\"updatedUtc\":\"$OLD\",\"isDeleted\":false}]}" | grep -q '"ignores":1' && ok "modification plus ancienne ignorée"

echo; echo "Tous les contrôles sont passés."
