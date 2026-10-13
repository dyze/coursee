@echo off
setlocal
cd /d "%~dp0"
set COVERAGE_DIR=artifacts\coverage

if exist "%COVERAGE_DIR%" rmdir /s /q "%COVERAGE_DIR%"
dotnet restore || exit /b 1
dotnet tool restore || exit /b 1
dotnet build -c Release --no-restore || exit /b 1
rem Fails when Core line coverage is below the threshold set in the test project.
rem -tl:off: the terminal logger would hide the coverage summary.
dotnet test -c Release --no-build -tl:off
set TEST_RESULT=%errorlevel%

rem The report is built even when the threshold failed, to show what is missing.
if not exist "%COVERAGE_DIR%\coverage.cobertura.xml" (
    echo No coverage data produced.
    exit /b 1
)
dotnet reportgenerator "-reports:%COVERAGE_DIR%\coverage.cobertura.xml" "-targetdir:%COVERAGE_DIR%\report" -reporttypes:Html || exit /b 1
echo Coverage report: %COVERAGE_DIR%\report\index.html
if /i "%~1"=="open" start "" "%COVERAGE_DIR%\report\index.html"
exit /b %TEST_RESULT%
