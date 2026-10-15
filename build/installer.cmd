@echo off
rem Builds the coursee installer and portable zip, in artifacts\installer:
rem coursee-setup-<version>.exe and coursee-portable-<version>.zip
rem Needs the .NET 10 SDK and Inno Setup 6.3 or later (https://jrsoftware.org/isinfo.php).
setlocal
cd /d "%~dp0.."

set "PROJECT=src\coursee.App\coursee.App.csproj"
set "PUBLISH=%CD%\artifacts\installer\app"

rem The version comes from Directory.Build.props, the single source of truth.
set "VERSION="
for /f "usebackq delims=" %%v in (`dotnet msbuild "%PROJECT%" -nologo -getProperty:Version`) do set "VERSION=%%v"
if not defined VERSION (
  echo Could not read the version from Directory.Build.props.
  exit /b 1
)

rem Inno Setup compiler: on PATH, or in the default install folders (all users, then per user).
set "ISCC="
for /f "delims=" %%i in ('where ISCC.exe 2^>nul') do if not defined ISCC set "ISCC=%%i"
if not defined ISCC if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not defined ISCC (
  echo Inno Setup was not found. Install it from https://jrsoftware.org/isinfo.php, or put ISCC.exe on PATH.
  exit /b 1
)

echo Building the coursee %VERSION% installer...

rem Self-contained: the installed app needs no .NET runtime on the PC. A single file (see coursee.App.csproj).
if exist "%PUBLISH%" rmdir /s /q "%PUBLISH%"
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -o "%PUBLISH%" || exit /b 1

"%ISCC%" /Q "/DAppVersion=%VERSION%" "/DPublishDir=%PUBLISH%" "build\installer\coursee.iss" || exit /b 1

rem Portable zip: the same files in a coursee folder, with the marker that keeps the data beside coursee.exe.
set "PORTABLE=%CD%\artifacts\installer\portable\coursee"
set "ZIP=%CD%\artifacts\installer\coursee-portable-%VERSION%.zip"
if exist "%CD%\artifacts\installer\portable" rmdir /s /q "%CD%\artifacts\installer\portable"
xcopy "%PUBLISH%" "%PORTABLE%\" /e /i /q >nul || exit /b 1
echo coursee keeps its settings, entries and logs in the data folder next to coursee.exe while this file is here.> "%PORTABLE%\portable.txt"
if exist "%ZIP%" del "%ZIP%"
powershell -NoProfile -Command "Compress-Archive -Path $env:PORTABLE -DestinationPath $env:ZIP" || exit /b 1

echo.
echo Installer: artifacts\installer\coursee-setup-%VERSION%.exe
echo Portable:  artifacts\installer\coursee-portable-%VERSION%.zip
