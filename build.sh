#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dotnet restore
dotnet build -c Release --no-restore
# -tl:off: the terminal logger would hide the coverage summary.
dotnet test -c Release --no-build -tl:off
