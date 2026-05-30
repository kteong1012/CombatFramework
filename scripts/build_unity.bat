@echo off
setlocal
set PROJ_DIR=%~dp0..\src
set OUT_DIR=%~dp0..\dist
set PLUGINS_DIR=%~dp0..\..\Meow\Assets\Plugins\CombatFramework

echo == CombatFramework Unity Build ==
echo Target: net48 / Debug

dotnet build "%PROJ_DIR%\CombatFramework.csproj" -f net48 -c Debug -o "%OUT_DIR%" --nologo
if %ERRORLEVEL% neq 0 (
    echo [FAILED] dotnet build returned %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo.
echo == Deploy to Unity Plugins ==
if not exist "%PLUGINS_DIR%" mkdir "%PLUGINS_DIR%"
copy /Y "%OUT_DIR%\CombatFramework.dll" "%PLUGINS_DIR%" >nul
copy /Y "%OUT_DIR%\CombatFramework.pdb" "%PLUGINS_DIR%" >nul
copy /Y "%OUT_DIR%\MoonSharp.Interpreter.dll" "%PLUGINS_DIR%" >nul
echo Deployed to %PLUGINS_DIR%

echo.
echo == Done ==
