#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENVIRONMENT="${PERFI_CONSOLE_ENVIRONMENT:-Development}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/create-user.sh <username> <password>

Creates a new PerFi login (ASP.NET Core Identity user) against the database configured
for the "Development" environment (per-fi-db-server.database.windows.net).

Optional environment variables:
  PERFI_CONSOLE_ENVIRONMENT  Environment used to resolve the connection string
                             (default: Development).
EOF
}

if [[ $# -lt 2 || "$1" == "-h" || "$1" == "--help" ]]; then
  usage >&2
  exit 1
fi

USERNAME="$1"
PASSWORD="$2"

DOTNET_ENVIRONMENT="${ENVIRONMENT}" ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}" \
  dotnet run --project "${ROOT}/PerFi.Console/PerFi.Console.csproj" -- create-user "${USERNAME}" "${PASSWORD}"
