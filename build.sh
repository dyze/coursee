#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
