@echo off
REM Lance l'API sur Windows : http://localhost:5080 (admin : /admin/)
cd /d "%~dp0"
dotnet restore --configfile NuGet.config
if errorlevel 1 (
  echo.
  echo Echec de la restauration : verifiez la connexion Internet ^(acces a api.nuget.org^).
  pause
  exit /b 1
)
set ASPNETCORE_ENVIRONMENT=Development
start "" http://localhost:5080/admin/
dotnet run --launch-profile "EduHorsLigne.Api (Windows)"
pause
