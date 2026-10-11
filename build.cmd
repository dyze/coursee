@echo off
setlocal
dotnet restore || exit /b 1
dotnet build -c Release --no-restore || exit /b 1
dotnet test -c Release --no-build || exit /b 1
