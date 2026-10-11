@echo off
rem Builds the coursee installer: artifacts\installer\coursee-setup-<version>.exe
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

rem Self-contained: the installed app needs no .NET runtime on the PC.
if exist "%PUBLISH%" rmdir /s /q "%PUBLISH%"
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -o "%PUBLISH%" || exit /b 1

"%ISCC%" /Q "/DAppVersion=%VERSION%" "/DPublishDir=%PUBLISH%" "build\installer\coursee.iss" || exit /b 1

echo.
echo Installer: artifacts\installer\coursee-setup-%VERSION%.exe
