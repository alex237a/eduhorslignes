@echo off
REM ============================================================
REM  Compile puis lance l'application Windows (sans passer par "-t:Run", instable pour MAUI non empaquete).
REM  Affiche le journal de demarrage (startup.log) si l'application se ferme avec une erreur.
REM ============================================================
cd /d "%~dp0EduHorsLigne"
echo [1/2] Compilation de l'application...
dotnet build -f net10.0-windows10.0.19041.0 -c Debug
if errorlevel 1 (
  echo.
  echo Echec de la compilation. Corrigez les erreurs ci-dessus.
  pause
  exit /b 1
)

set "EXE="
for /f "delims=" %%F in ('dir /s /b "%~dp0EduHorsLigne\bin\Debug\EduHorsLigne.exe" 2^>nul') do set "EXE=%%F"
if not defined EXE (
  echo EduHorsLigne.exe introuvable dans bin\Debug.
  pause
  exit /b 1
)
for %%F in ("%EXE%") do set "OUT=%%~dpF"
if exist "%OUT%startup.log" del "%OUT%startup.log"

echo [2/2] Lancement : %EXE%
pushd "%OUT%"
start "" /wait "%EXE%"
set "CODE=%errorlevel%"
popd

echo.
echo L'application s'est fermee (code %CODE%).
if exist "%OUT%startup.log" (
  echo ------------------ startup.log ------------------
  type "%OUT%startup.log"
  echo -------------------------------------------------
  echo Fichier : %OUT%startup.log
)
pause
