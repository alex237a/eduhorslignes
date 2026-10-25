@echo off
REM ============================================================
REM  Lance l'API (fenetre separee) puis l'application sur Windows.
REM  Prerequis : SDK .NET 10 + charge de travail MAUI, acces Internet au 1er lancement.
REM ============================================================
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Development

echo [1/3] Demarrage de l'API sur http://localhost:5080 ...
start "EduHorsLigne API" cmd /k "cd /d %~dp0EduHorsLigne.Api && dotnet run --no-launch-profile --urls http://localhost:5080"

echo [2/3] Attente du demarrage de l'API (maximum 2 minutes)...
set /a TRIES=0
:wait
set /a TRIES+=1
if %TRIES% gtr 40 (
  echo L'API ne repond pas. Regardez la fenetre "EduHorsLigne API" pour voir l'erreur.
  pause
  exit /b 1
)
timeout /t 3 /nobreak >nul
curl -s -o nul -f http://localhost:5080/api/health
if errorlevel 1 goto wait
echo API prete : http://localhost:5080  ^(administration : /admin/^)

echo [3/3] Application...
call "%~dp0run-app.bat"
